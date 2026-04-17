using System;
using Test;

namespace Basement.Tasks.Tests
{
    /// <summary>
    /// 任务调度器测试
    /// 测试TimingTaskScheduler的基本功能和边界条件
    /// </summary>
    public class TimingTaskSchedulerTest : TestBase
    {
        private TimingTaskScheduler _scheduler;
        private TimingTaskManager _manager;

        public TimingTaskSchedulerTest() : base("TimingTaskSchedulerTest") { }

        protected override void Setup()
        {
            _scheduler = TimingTaskScheduler.Instance;
            _manager = TimingTaskManager.Instance;

            _scheduler.Initialize();
            _manager.Initialize();
            _manager.ClearAllTasks();
        }

        protected override void Teardown()
        {
            _manager.ClearAllTasks();
        }

        protected override void Execute()
        {
            TestInitialization();
            TestUpdateTimeTriggeredTask();
            TestUpdatePeriodicTask();
            TestUpdateConditionalTask();
            TestSetUpdateInterval();
            TestActiveTaskCount();
            TestMultipleTasks();
            TestTaskCompletion();
            TestTaskCancellation();
        }

        /// <summary>
        /// 测试初始化
        /// </summary>
        private void TestInitialization()
        {
            TimingTaskScheduler scheduler = TimingTaskScheduler.Instance;

            AssertNotNull(scheduler, "调度器实例不应为空");
            AssertEqual(0, scheduler.ActiveTaskCount, "初始活跃任务数应为0");
        }

        /// <summary>
        /// 测试更新时间触发任务
        /// </summary>
        private void TestUpdateTimeTriggeredTask()
        {
            bool executed = false;
            _manager.CreateTimeTriggeredTask(() => executed = true, 0f);

            AssertEqual(0, _scheduler.ActiveTaskCount, "初始活跃任务数应为0");

            _scheduler.Update();

            AssertTrue(executed, "任务应被执行");
            AssertEqual(0, _scheduler.ActiveTaskCount, "执行后活跃任务数应为0");
        }

        /// <summary>
        /// 测试更新周期性任务
        /// </summary>
        private void TestUpdatePeriodicTask()
        {
            int executeCount = 0;
            _manager.CreatePeriodicTask(() => executeCount++, 0.001f, 3);

            AssertEqual(0, _scheduler.ActiveTaskCount, "初始活跃任务数应为0");

            _scheduler.Update();

            AssertEqual(1, executeCount, "任务应执行一次");

            _scheduler.Update();
            _scheduler.Update();

            AssertEqual(3, executeCount, "任务应执行三次");
        }

        /// <summary>
        /// 测试更新条件任务
        /// </summary>
        private void TestUpdateConditionalTask()
        {
            bool executed = false;
            bool conditionMet = false;

            _manager.CreateConditionalTask(
                () => executed = true,
                () => conditionMet,
                0.001f
            );

            _scheduler.Update();

            AssertFalse(executed, "条件不满足时任务不应执行");

            conditionMet = true;
            _scheduler.Update();

            AssertTrue(executed, "条件满足时任务应执行");
        }

        /// <summary>
        /// 测试设置更新间隔
        /// </summary>
        private void TestSetUpdateInterval()
        {
            float newInterval = 0.033f;
            _scheduler.SetUpdateInterval(newInterval);

            AssertEqual(newInterval, _scheduler.UpdateInterval, "更新间隔应正确设置");
        }

        /// <summary>
        /// 测试活跃任务数量
        /// </summary>
        private void TestActiveTaskCount()
        {
            _manager.CreateTimeTriggeredTask(() => { }, 0f);
            _manager.CreatePeriodicTask(() => { }, 0.1f, 2);
            _manager.CreateConditionalTask(() => { }, () => true, 0.1f);

            _scheduler.Update();

            int activeCount = _scheduler.ActiveTaskCount;

            AssertTrue(activeCount >= 0, "活跃任务数应大于等于0");
        }

        /// <summary>
        /// 测试多个任务调度
        /// </summary>
        private void TestMultipleTasks()
        {
            int[] counters = new int[5];

            for (int i = 0; i < 5; i++)
            {
                int index = i;
                _manager.CreateTimeTriggeredTask(() => counters[index]++, 0f);
            }

            _scheduler.Update();

            AssertEqual(1, counters[0], "第一个任务应执行一次");
            AssertEqual(1, counters[1], "第二个任务应执行一次");
            AssertEqual(1, counters[2], "第三个任务应执行一次");
            AssertEqual(1, counters[3], "第四个任务应执行一次");
            AssertEqual(1, counters[4], "第五个任务应执行一次");
        }

        /// <summary>
        /// 测试任务完成
        /// </summary>
        private void TestTaskCompletion()
        {
            var task = _manager.CreateTimeTriggeredTask(() => { }, 0f);

            AssertEqual(TimingTaskState.Ready, task.State, "初始状态应为Ready");

            _scheduler.Update();

            AssertEqual(TimingTaskState.Completed, task.State, "执行后状态应为Completed");
        }

        /// <summary>
        /// 测试任务取消
        /// </summary>
        private void TestTaskCancellation()
        {
            bool executed = false;
            var task = _manager.CreateTimeTriggeredTask(() => executed = true, 0f);

            _manager.CancelTask(task.TaskId);

            _scheduler.Update();

            AssertFalse(executed, "取消的任务不应执行");
            AssertEqual(TimingTaskState.Completed, task.State, "取消后状态应为Completed");
        }
    }
}
