using System;
using Test;

namespace Basement.Tasks.Tests
{
    /// <summary>
    /// 周期性任务测试
    /// 测试PeriodicTask的基本功能和边界条件
    /// </summary>
    public class PeriodicTaskTest : TestBase
    {
        public PeriodicTaskTest() : base("PeriodicTaskTest") { }

        protected override void Execute()
        {
            TestBasicExecution();
            TestInterval();
            TestRepeatCount();
            TestInfiniteRepeat();
            TestFiniteRepeat();
            TestCurrentRepeat();
            TestPriority();
            TestTaskId();
            TestCallbacks();
            TestCancel();
            TestStateTransitions();
            TestNullAction();
        }

        /// <summary>
        /// 测试基本执行功能
        /// </summary>
        private void TestBasicExecution()
        {
            int executeCount = 0;
            PeriodicTask task = new PeriodicTask(() => executeCount++, 0.1f, 3);

            AssertEqual(TimingTaskState.Ready, task.State, "初始状态应为Ready");
            AssertEqual(TimingTaskPriority.Normal, task.Priority, "默认优先级应为Normal");

            task.Execute();
            AssertEqual(1, executeCount, "任务应执行一次");
            AssertEqual(TimingTaskState.Ready, task.State, "未完成时状态应为Ready");

            task.Execute();
            AssertEqual(2, executeCount, "任务应执行两次");

            task.Execute();
            AssertEqual(3, executeCount, "任务应执行三次");
            AssertEqual(TimingTaskState.Completed, task.State, "完成后状态应为Completed");
        }

        /// <summary>
        /// 测试间隔时间设置
        /// </summary>
        private void TestInterval()
        {
            float interval = 0.5f;
            PeriodicTask task = new PeriodicTask(() => { }, interval);

            AssertEqual(interval, task.Interval, "间隔时间应正确设置");
            AssertEqual(interval, task.DelayTime, "DelayTime应等于Interval");
        }

        /// <summary>
        /// 测试重复次数设置
        /// </summary>
        private void TestRepeatCount()
        {
            PeriodicTask finiteTask = new PeriodicTask(() => { }, 0.1f, 5);
            PeriodicTask infiniteTask = new PeriodicTask(() => { }, 0.1f, -1);

            AssertEqual(5, finiteTask.RepeatCount, "有限重复次数应正确设置");
            AssertEqual(-1, infiniteTask.RepeatCount, "无限重复次数应为-1");
        }

        /// <summary>
        /// 测试无限重复
        /// </summary>
        private void TestInfiniteRepeat()
        {
            int executeCount = 0;
            PeriodicTask task = new PeriodicTask(() => executeCount++, 0.1f, -1);

            for (int i = 0; i < 10; i++)
            {
                task.Execute();
            }

            AssertEqual(10, executeCount, "无限重复任务应持续执行");
            AssertEqual(TimingTaskState.Ready, task.State, "无限重复任务状态应为Ready");
        }

        /// <summary>
        /// 测试有限重复
        /// </summary>
        private void TestFiniteRepeat()
        {
            int executeCount = 0;
            PeriodicTask task = new PeriodicTask(() => executeCount++, 0.1f, 3);

            for (int i = 0; i < 10; i++)
            {
                task.Execute();
            }

            AssertEqual(3, executeCount, "有限重复任务应只执行指定次数");
            AssertEqual(TimingTaskState.Completed, task.State, "完成后状态应为Completed");
        }

        /// <summary>
        /// 测试当前重复次数
        /// </summary>
        private void TestCurrentRepeat()
        {
            PeriodicTask task = new PeriodicTask(() => { }, 0.1f, 5);

            AssertEqual(0, task.CurrentRepeat, "初始当前重复次数应为0");

            task.Execute();
            AssertEqual(1, task.CurrentRepeat, "执行一次后当前重复次数应为1");

            task.Execute();
            AssertEqual(2, task.CurrentRepeat, "执行两次后当前重复次数应为2");
        }

        /// <summary>
        /// 测试优先级设置
        /// </summary>
        private void TestPriority()
        {
            PeriodicTask highPriorityTask = new PeriodicTask(() => { }, 0.1f, 1, TimingTaskPriority.High);
            PeriodicTask lowPriorityTask = new PeriodicTask(() => { }, 0.1f, 1, TimingTaskPriority.Low);

            AssertEqual(TimingTaskPriority.High, highPriorityTask.Priority, "高优先级任务应正确设置");
            AssertEqual(TimingTaskPriority.Low, lowPriorityTask.Priority, "低优先级任务应正确设置");
        }

        /// <summary>
        /// 测试任务ID
        /// </summary>
        private void TestTaskId()
        {
            PeriodicTask task1 = new PeriodicTask(() => { }, 0.1f);
            PeriodicTask task2 = new PeriodicTask(() => { }, 0.1f);

            AssertNotNull(task1.TaskId, "任务ID不应为空");
            AssertNotNull(task2.TaskId, "任务ID不应为空");
            AssertTrue(task1.TaskId != task2.TaskId, "不同任务应有不同ID");

            string customId = "custom-periodic-task-123";
            PeriodicTask task3 = new PeriodicTask(customId, () => { }, 0.1f);
            AssertEqual(customId, task3.TaskId, "自定义任务ID应正确设置");
        }

        /// <summary>
        /// 测试回调函数
        /// </summary>
        private void TestCallbacks()
        {
            bool onCompletedCalled = false;
            bool onCancelledCalled = false;
            Exception caughtException = null;

            PeriodicTask task = new PeriodicTask(() => { }, 0.1f, 1);
            task.OnCompleted(() => onCompletedCalled = true);
            task.OnCancelled(() => onCancelledCalled = true);
            task.OnFailed((ex) => caughtException = ex);

            task.Execute();

            AssertTrue(onCompletedCalled, "OnCompleted回调应被调用");
            AssertFalse(onCancelledCalled, "OnCancelled回调不应被调用");
            AssertNull(caughtException, "OnFailed回调不应被调用");
        }

        /// <summary>
        /// 测试取消任务
        /// </summary>
        private void TestCancel()
        {
            int executeCount = 0;
            bool onCancelledCalled = false;

            PeriodicTask task = new PeriodicTask(() => executeCount++, 0.1f, 10);
            task.OnCancelled(() => onCancelledCalled = true);

            task.Execute();
            AssertEqual(1, executeCount, "任务应执行一次");

            task.Cancel();

            AssertEqual(TimingTaskState.Completed, task.State, "取消后状态应为Completed");
            AssertEqual(1, executeCount, "取消后不应继续执行");
            AssertTrue(onCancelledCalled, "OnCancelled回调应被调用");

            bool secondCancelCalled = false;
            task.OnCancelled(() => secondCancelCalled = true);
            task.Cancel();

            AssertFalse(secondCancelCalled, "已完成的任务取消时不应触发回调");
        }

        /// <summary>
        /// 测试状态转换
        /// </summary>
        private void TestStateTransitions()
        {
            PeriodicTask task = new PeriodicTask(() => { }, 0.1f, 2);

            AssertEqual(TimingTaskState.Ready, task.State, "初始状态应为Ready");

            task.Execute();
            AssertEqual(TimingTaskState.Ready, task.State, "未完成时状态应为Ready");

            task.Execute();
            AssertEqual(TimingTaskState.Completed, task.State, "完成后状态应为Completed");

            task.Execute();
            AssertEqual(TimingTaskState.Completed, task.State, "已完成的任务再次执行状态仍为Completed");
        }

        /// <summary>
        /// 测试空Action异常
        /// </summary>
        private void TestNullAction()
        {
            AssertThrows<ArgumentNullException>(() => {
                new PeriodicTask(null, 0.1f);
            }, "创建空Action的任务应抛出ArgumentNullException");

            AssertThrows<ArgumentNullException>(() => {
                new PeriodicTask("test-id", null, 0.1f);
            }, "创建空Action的任务应抛出ArgumentNullException");
        }
    }
}
