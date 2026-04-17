using System;
using Test;

namespace Basement.Tasks.Tests
{
    /// <summary>
    /// 时间触发任务测试
    /// 测试TimeTriggeredTask的基本功能和边界条件
    /// </summary>
    public class TimeTriggeredTaskTest : TestBase
    {
        public TimeTriggeredTaskTest() : base("TimeTriggeredTaskTest") { }

        protected override void Execute()
        {
            TestBasicExecution();
            TestDelayTime();
            TestPriority();
            TestTaskId();
            TestCallbacks();
            TestCancel();
            TestStateTransitions();
            TestScheduledTask();
            TestNullAction();
        }

        /// <summary>
        /// 测试基本执行功能
        /// </summary>
        private void TestBasicExecution()
        {
            bool executed = false;
            TimeTriggeredTask task = new TimeTriggeredTask(() => executed = true, 0f);

            AssertEqual(TimingTaskState.Ready, task.State, "初始状态应为Ready");
            AssertEqual(TimingTaskPriority.Normal, task.Priority, "默认优先级应为Normal");

            task.Execute();

            AssertTrue(executed, "任务应被执行");
            AssertEqual(TimingTaskState.Completed, task.State, "执行后状态应为Completed");
        }

        /// <summary>
        /// 测试延迟时间设置
        /// </summary>
        private void TestDelayTime()
        {
            float delayTime = 1.5f;
            TimeTriggeredTask task = new TimeTriggeredTask(() => { }, delayTime);

            AssertEqual(delayTime, task.DelayTime, "延迟时间应正确设置");
        }

        /// <summary>
        /// 测试优先级设置
        /// </summary>
        private void TestPriority()
        {
            TimeTriggeredTask highPriorityTask = new TimeTriggeredTask(() => { }, 0f, TimingTaskPriority.High);
            TimeTriggeredTask lowPriorityTask = new TimeTriggeredTask(() => { }, 0f, TimingTaskPriority.Low);

            AssertEqual(TimingTaskPriority.High, highPriorityTask.Priority, "高优先级任务应正确设置");
            AssertEqual(TimingTaskPriority.Low, lowPriorityTask.Priority, "低优先级任务应正确设置");
        }

        /// <summary>
        /// 测试任务ID
        /// </summary>
        private void TestTaskId()
        {
            TimeTriggeredTask task1 = new TimeTriggeredTask(() => { }, 0f);
            TimeTriggeredTask task2 = new TimeTriggeredTask(() => { }, 0f);

            AssertNotNull(task1.TaskId, "任务ID不应为空");
            AssertNotNull(task2.TaskId, "任务ID不应为空");
            AssertTrue(task1.TaskId != task2.TaskId, "不同任务应有不同ID");

            string customId = "custom-task-123";
            TimeTriggeredTask task3 = new TimeTriggeredTask(customId, () => { }, 0f);
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

            TimeTriggeredTask task = new TimeTriggeredTask(() => { }, 0f);
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
            bool executed = false;
            bool onCancelledCalled = false;

            TimeTriggeredTask task = new TimeTriggeredTask(() => executed = true, 0f);
            task.OnCancelled(() => onCancelledCalled = true);

            task.Cancel();

            AssertEqual(TimingTaskState.Completed, task.State, "取消后状态应为Completed");
            AssertFalse(executed, "取消的任务不应执行");
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
            TimeTriggeredTask task = new TimeTriggeredTask(() => { }, 0f);

            AssertEqual(TimingTaskState.Ready, task.State, "初始状态应为Ready");

            task.Execute();

            AssertEqual(TimingTaskState.Completed, task.State, "执行后状态应为Completed");

            task.Execute();

            AssertEqual(TimingTaskState.Completed, task.State, "已完成的任务再次执行状态仍为Completed");
        }

        /// <summary>
        /// 测试定时任务创建
        /// </summary>
        private void TestScheduledTask()
        {
            bool executed = false;
            DateTime scheduledTime = DateTime.Now.AddSeconds(0.1f);

            TimeTriggeredTask task = TimeTriggeredTask.CreateScheduledTask(() => executed = true, scheduledTime);

            AssertNotNull(task, "定时任务应成功创建");
            AssertTrue(task.DelayTime >= 0, "延迟时间应大于等于0");
            AssertEqual(TimingTaskState.Ready, task.State, "初始状态应为Ready");
        }

        /// <summary>
        /// 测试空Action异常
        /// </summary>
        private void TestNullAction()
        {
            AssertThrows<ArgumentNullException>(() => {
                new TimeTriggeredTask(null, 0f);
            }, "创建空Action的任务应抛出ArgumentNullException");

            AssertThrows<ArgumentNullException>(() => {
                new TimeTriggeredTask("test-id", null, 0f);
            }, "创建空Action的任务应抛出ArgumentNullException");
        }
    }
}
