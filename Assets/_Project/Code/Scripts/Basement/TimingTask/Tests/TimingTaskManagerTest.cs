using System;
using Test;

namespace Basement.Tasks.Tests
{
    /// <summary>
    /// 任务管理器测试
    /// 测试TimingTaskManager的基本功能和边界条件
    /// </summary>
    public class TimingTaskManagerTest : TestBase
    {
        private TimingTaskManager _manager;

        public TimingTaskManagerTest() : base("TimingTaskManagerTest") { }

        protected override void Setup()
        {
            _manager = TimingTaskManager.Instance;
            _manager.Initialize();
            _manager.ClearAllTasks();
        }

        protected override void Teardown()
        {
            _manager.ClearAllTasks();
        }

        protected override void Execute()
        {
            TestCreateTimeTriggeredTask();
            TestCreateScheduledTask();
            TestCreatePeriodicTask();
            TestCreateConditionalTask();
            TestCancelTask();
            TestGetTask();
            TestGetAllTasks();
            TestClearAllTasks();
            TestClearCompletedTasks();
            TestGetStatistics();
            TestTaskPrioritySorting();
            TestMultipleTasks();
            TestNullTaskId();
        }

        /// <summary>
        /// 测试创建时间触发任务
        /// </summary>
        private void TestCreateTimeTriggeredTask()
        {
            bool executed = false;
            TimeTriggeredTask task = _manager.CreateTimeTriggeredTask(() => executed = true, 0f);

            AssertNotNull(task, "任务应成功创建");
            AssertEqual(TimingTaskState.Ready, task.State, "任务状态应为Ready");
            AssertEqual(0f, task.DelayTime, "延迟时间应正确设置");

            task.Execute();
            AssertTrue(executed, "任务应执行");
        }

        /// <summary>
        /// 测试创建定时任务
        /// </summary>
        private void TestCreateScheduledTask()
        {
            bool executed = false;
            DateTime scheduledTime = DateTime.Now.AddSeconds(0.1f);
            TimeTriggeredTask task = _manager.CreateScheduledTask(() => executed = true, scheduledTime);

            AssertNotNull(task, "任务应成功创建");
            AssertEqual(TimingTaskState.Ready, task.State, "任务状态应为Ready");
            AssertTrue(task.DelayTime >= 0, "延迟时间应大于等于0");
        }

        /// <summary>
        /// 测试创建周期性任务
        /// </summary>
        private void TestCreatePeriodicTask()
        {
            int executeCount = 0;
            PeriodicTask task = _manager.CreatePeriodicTask(() => executeCount++, 0.1f, 3);

            AssertNotNull(task, "任务应成功创建");
            AssertEqual(TimingTaskState.Ready, task.State, "任务状态应为Ready");
            AssertEqual(0.1f, task.Interval, "间隔时间应正确设置");

            task.Execute();
            task.Execute();
            task.Execute();

            AssertEqual(3, executeCount, "任务应执行3次");
        }

        /// <summary>
        /// 测试创建条件任务
        /// </summary>
        private void TestCreateConditionalTask()
        {
            bool executed = false;
            ConditionalTask task = _manager.CreateConditionalTask(
                () => executed = true,
                () => true,
                0.1f
            );

            AssertNotNull(task, "任务应成功创建");
            AssertEqual(TimingTaskState.Ready, task.State, "任务状态应为Ready");
            AssertEqual(0.1f, task.CheckInterval, "检查间隔应正确设置");

            task.Execute();
            AssertTrue(executed, "任务应执行");
        }

        /// <summary>
        /// 测试取消任务
        /// </summary>
        private void TestCancelTask()
        {
            bool executed = false;
            TimeTriggeredTask task = _manager.CreateTimeTriggeredTask(() => executed = true, 0f);
            string taskId = task.TaskId;

            bool cancelResult = _manager.CancelTask(taskId);

            AssertTrue(cancelResult, "取消任务应返回true");
            AssertEqual(TimingTaskState.Completed, task.State, "取消后任务状态应为Completed");

            task.Execute();
            AssertFalse(executed, "取消的任务不应执行");

            bool cancelAgain = _manager.CancelTask(taskId);
            AssertFalse(cancelAgain, "取消已取消的任务应返回false");
        }

        /// <summary>
        /// 测试获取任务
        /// </summary>
        private void TestGetTask()
        {
            TimeTriggeredTask task = _manager.CreateTimeTriggeredTask(() => { }, 0f);
            string taskId = task.TaskId;

            ITimingTask retrievedTask = _manager.GetTask(taskId);

            AssertNotNull(retrievedTask, "应能获取到任务");
            AssertEqual(taskId, retrievedTask.TaskId, "获取的任务ID应匹配");

            ITimingTask nullTask = _manager.GetTask("non-existent-id");
            AssertNull(nullTask, "不存在的任务应返回null");
        }

        /// <summary>
        /// 测试获取所有任务
        /// </summary>
        private void TestGetAllTasks()
        {
            _manager.CreateTimeTriggeredTask(() => { }, 0f);
            _manager.CreatePeriodicTask(() => { }, 0.1f, 1);
            _manager.CreateConditionalTask(() => { }, () => true, 0.1f);

            var allTasks = _manager.GetAllTasks();

            AssertEqual(3, allTasks.Count, "应获取到3个任务");
        }

        /// <summary>
        /// 测试清空所有任务
        /// </summary>
        private void TestClearAllTasks()
        {
            _manager.CreateTimeTriggeredTask(() => { }, 0f);
            _manager.CreatePeriodicTask(() => { }, 0.1f, 1);
            _manager.CreateConditionalTask(() => { }, () => true, 0.1f);

            AssertEqual(3, _manager.GetAllTasks().Count, "应创建3个任务");

            _manager.ClearAllTasks();

            AssertEqual(0, _manager.GetAllTasks().Count, "所有任务应被清空");
        }

        /// <summary>
        /// 测试清空已完成任务
        /// </summary>
        private void TestClearCompletedTasks()
        {
            var task1 = _manager.CreateTimeTriggeredTask(() => { }, 0f);
            var task2 = _manager.CreateTimeTriggeredTask(() => { }, 0f);
            var task3 = _manager.CreatePeriodicTask(() => { }, 0.1f, 2);

            task1.Execute();
            task2.Execute();

            AssertEqual(3, _manager.GetAllTasks().Count, "应有3个任务");

            _manager.ClearCompletedTasks();

            AssertEqual(1, _manager.GetAllTasks().Count, "应剩余1个未完成任务");
            AssertEqual(task3.TaskId, _manager.GetAllTasks()[0].TaskId, "剩余任务应为未完成的周期性任务");
        }

        /// <summary>
        /// 测试获取统计信息
        /// </summary>
        private void TestGetStatistics()
        {
            var task1 = _manager.CreateTimeTriggeredTask(() => { }, 0f);
            var task2 = _manager.CreateTimeTriggeredTask(() => { }, 0f);
            var task3 = _manager.CreatePeriodicTask(() => { }, 0.1f, 2);

            task1.Execute();

            TimingTaskStatistics stats = _manager.GetStatistics();

            AssertEqual(3, stats.TotalCount, "总任务数应为3");
            AssertEqual(2, stats.ReadyCount, "就绪任务数应为2");
            AssertEqual(0, stats.RunningCount, "运行中任务数应为0");
            AssertEqual(1, stats.CompletedCount, "已完成任务数应为1");
        }

        /// <summary>
        /// 测试任务优先级排序
        /// </summary>
        private void TestTaskPrioritySorting()
        {
            _manager.CreateTimeTriggeredTask(() => { }, 1f, TimingTaskPriority.Low);
            _manager.CreateTimeTriggeredTask(() => { }, 2f, TimingTaskPriority.High);
            _manager.CreateTimeTriggeredTask(() => { }, 0.5f, TimingTaskPriority.Normal);

            var allTasks = _manager.GetAllTasks();

            AssertEqual(3, allTasks.Count, "应有3个任务");
            AssertEqual(TimingTaskPriority.High, allTasks[0].Priority, "第一个任务应为高优先级");
            AssertEqual(TimingTaskPriority.Normal, allTasks[1].Priority, "第二个任务应为普通优先级");
            AssertEqual(TimingTaskPriority.Low, allTasks[2].Priority, "第三个任务应为低优先级");
        }

        /// <summary>
        /// 测试多个任务管理
        /// </summary>
        private void TestMultipleTasks()
        {
            int[] counters = new int[5];

            for (int i = 0; i < 5; i++)
            {
                int index = i;
                _manager.CreateTimeTriggeredTask(() => counters[index]++, 0f);
            }

            var allTasks = _manager.GetAllTasks();

            AssertEqual(5, allTasks.Count, "应创建5个任务");

            foreach (var task in allTasks)
            {
                task.Execute();
            }

            AssertEqual(1, counters[0], "第一个任务应执行一次");
            AssertEqual(1, counters[1], "第二个任务应执行一次");
            AssertEqual(1, counters[2], "第三个任务应执行一次");
            AssertEqual(1, counters[3], "第四个任务应执行一次");
            AssertEqual(1, counters[4], "第五个任务应执行一次");
        }

        /// <summary>
        /// 测试空任务ID
        /// </summary>
        private void TestNullTaskId()
        {
            bool cancelNull = _manager.CancelTask(null);
            AssertFalse(cancelNull, "取消null任务ID应返回false");

            bool cancelEmpty = _manager.CancelTask("");
            AssertFalse(cancelEmpty, "取消空任务ID应返回false");

            ITimingTask getNull = _manager.GetTask(null);
            AssertNull(getNull, "获取null任务ID应返回null");

            ITimingTask getEmpty = _manager.GetTask("");
            AssertNull(getEmpty, "获取空任务ID应返回null");
        }
    }
}
