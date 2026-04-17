using System;
using Test;

namespace Basement.Tasks.Tests
{
    /// <summary>
    /// 条件任务测试
    /// 测试ConditionalTask的基本功能和边界条件
    /// </summary>
    public class ConditionalTaskTest : TestBase
    {
        public ConditionalTaskTest() : base("ConditionalTaskTest") { }

        protected override void Execute()
        {
            TestBasicExecution();
            TestConditionTrue();
            TestConditionFalse();
            TestCheckInterval();
            TestPriority();
            TestTaskId();
            TestCallbacks();
            TestCancel();
            TestStateTransitions();
            TestNullAction();
            TestNullCondition();
        }

        /// <summary>
        /// 测试基本执行功能
        /// </summary>
        private void TestBasicExecution()
        {
            bool executed = false;
            bool conditionMet = true;

            ConditionalTask task = new ConditionalTask(
                () => executed = true,
                () => conditionMet,
                0.1f
            );

            AssertEqual(TimingTaskState.Ready, task.State, "初始状态应为Ready");
            AssertEqual(TimingTaskPriority.Normal, task.Priority, "默认优先级应为Normal");

            task.Execute();

            AssertTrue(executed, "任务应被执行");
            AssertEqual(TimingTaskState.Completed, task.State, "执行后状态应为Completed");
        }

        /// <summary>
        /// 测试条件为真时执行
        /// </summary>
        private void TestConditionTrue()
        {
            bool executed = false;
            int checkCount = 0;

            ConditionalTask task = new ConditionalTask(
                () => executed = true,
                () => {
                    checkCount++;
                    return true;
                },
                0.1f
            );

            task.Execute();

            AssertTrue(executed, "条件为真时应执行任务");
            AssertEqual(1, checkCount, "条件应被检查一次");
            AssertEqual(TimingTaskState.Completed, task.State, "执行后状态应为Completed");
        }

        /// <summary>
        /// 测试条件为假时不执行
        /// </summary>
        private void TestConditionFalse()
        {
            bool executed = false;
            int checkCount = 0;

            ConditionalTask task = new ConditionalTask(
                () => executed = true,
                () => {
                    checkCount++;
                    return false;
                },
                0.1f
            );

            task.Execute();

            AssertFalse(executed, "条件为假时不应执行任务");
            AssertEqual(1, checkCount, "条件应被检查一次");
            AssertEqual(TimingTaskState.Ready, task.State, "未执行时状态应为Ready");
        }

        /// <summary>
        /// 测试检查间隔设置
        /// </summary>
        private void TestCheckInterval()
        {
            float checkInterval = 0.5f;
            ConditionalTask task = new ConditionalTask(
                () => { },
                () => false,
                checkInterval
            );

            AssertEqual(checkInterval, task.CheckInterval, "检查间隔应正确设置");
            AssertEqual(checkInterval, task.DelayTime, "DelayTime应等于CheckInterval");
        }

        /// <summary>
        /// 测试优先级设置
        /// </summary>
        private void TestPriority()
        {
            ConditionalTask highPriorityTask = new ConditionalTask(
                () => { },
                () => true,
                0.1f,
                TimingTaskPriority.High
            );

            ConditionalTask lowPriorityTask = new ConditionalTask(
                () => { },
                () => true,
                0.1f,
                TimingTaskPriority.Low
            );

            AssertEqual(TimingTaskPriority.High, highPriorityTask.Priority, "高优先级任务应正确设置");
            AssertEqual(TimingTaskPriority.Low, lowPriorityTask.Priority, "低优先级任务应正确设置");
        }

        /// <summary>
        /// 测试任务ID
        /// </summary>
        private void TestTaskId()
        {
            ConditionalTask task1 = new ConditionalTask(
                () => { },
                () => true,
                0.1f
            );

            ConditionalTask task2 = new ConditionalTask(
                () => { },
                () => true,
                0.1f
            );

            AssertNotNull(task1.TaskId, "任务ID不应为空");
            AssertNotNull(task2.TaskId, "任务ID不应为空");
            AssertTrue(task1.TaskId != task2.TaskId, "不同任务应有不同ID");

            string customId = "custom-conditional-task-123";
            ConditionalTask task3 = new ConditionalTask(
                customId,
                () => { },
                () => true,
                0.1f
            );

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

            ConditionalTask task = new ConditionalTask(
                () => { },
                () => true,
                0.1f
            );

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

            ConditionalTask task = new ConditionalTask(
                () => executed = true,
                () => false,
                0.1f
            );

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
            bool conditionMet = false;

            ConditionalTask task = new ConditionalTask(
                () => { },
                () => conditionMet,
                0.1f
            );

            AssertEqual(TimingTaskState.Ready, task.State, "初始状态应为Ready");

            task.Execute();
            AssertEqual(TimingTaskState.Ready, task.State, "条件不满足时状态应为Ready");

            conditionMet = true;
            task.Execute();
            AssertEqual(TimingTaskState.Completed, task.State, "条件满足执行后状态应为Completed");

            task.Execute();
            AssertEqual(TimingTaskState.Completed, task.State, "已完成的任务再次执行状态仍为Completed");
        }

        /// <summary>
        /// 测试空Action异常
        /// </summary>
        private void TestNullAction()
        {
            AssertThrows<ArgumentNullException>(() => {
                new ConditionalTask(null, () => true, 0.1f);
            }, "创建空Action的任务应抛出ArgumentNullException");

            AssertThrows<ArgumentNullException>(() => {
                new ConditionalTask("test-id", null, () => true, 0.1f);
            }, "创建空Action的任务应抛出ArgumentNullException");
        }

        /// <summary>
        /// 测试空Condition异常
        /// </summary>
        private void TestNullCondition()
        {
            AssertThrows<ArgumentNullException>(() => {
                new ConditionalTask(() => { }, null, 0.1f);
            }, "创建空Condition的任务应抛出ArgumentNullException");

            AssertThrows<ArgumentNullException>(() => {
                new ConditionalTask("test-id", () => { }, null, 0.1f);
            }, "创建空Condition的任务应抛出ArgumentNullException");
        }
    }
}
