using System;
using System.Linq;
using System.Threading;
using Test;

namespace Basement.Threading.Tests
{
    /// <summary>
    /// TaskScheduler测试
    /// </summary>
    public class TaskSchedulerTest : TestBase
    {
        private TaskScheduler _taskScheduler;

        public TaskSchedulerTest() : base("TaskSchedulerTest") { }

        protected override void Setup()
        {
            // 初始化任务调度器
            _taskScheduler = TaskScheduler.Instance;
            _taskScheduler.Initialize();
        }

        protected override void Execute()
        {
            TestCreateTask();
            TestSubmitTask();
            TestTaskDependencies();
            TestCancelTask();
            TestClearAllTasks();
            TestGetStatistics();
        }

        protected override void Teardown()
        {
            // 清理所有任务
            _taskScheduler.ClearAllTasks();
        }

        /// <summary>
        /// 测试创建任务
        /// </summary>
        private void TestCreateTask()
        {
            bool executed = false;
            ITask task = _taskScheduler.CreateTask(() => executed = true);
            
            AssertNotNull(task, "Task should be created");
            AssertNotNull(task.Id, "Task should have an Id");
            AssertEqual(TaskStatus.Created, task.Status, "Task status should be Created");
        }

        /// <summary>
        /// 测试提交任务
        /// </summary>
        private void TestSubmitTask()
        {
            bool executed = false;
            ITask task = _taskScheduler.CreateTask(() => executed = true);
            
            // 记录提交前的状态
            TaskStatus initialStatus = task.Status;
            
            // 提交任务
            _taskScheduler.SubmitTask(task);
            
            // 等待任务执行
            // 由于任务在后台线程执行，我们需要等待足够的时间
            Thread.Sleep(300);
            
            // 验证任务状态已经更新
            AssertTrue(task.Status != initialStatus, "Task status should be updated after submission");
            
            // 验证任务状态不是Created
            AssertTrue(task.Status != TaskStatus.Created, "Task status should not be Created after submission");
        }

        /// <summary>
        /// 测试任务依赖
        /// </summary>
        private void TestTaskDependencies()
        {
            bool task1Executed = false;
            bool task2Executed = false;

            ITask task1 = _taskScheduler.CreateTask(() => task1Executed = true);
            ITask task2 = _taskScheduler.CreateTask(() => task2Executed = true);

            // 添加依赖关系：task2依赖于task1
            task2.AddDependency(task1);
            
            // 提交任务
            _taskScheduler.SubmitTask(task1);
            _taskScheduler.SubmitTask(task2);
            
            // 等待任务执行
            // 由于任务在后台线程执行，我们需要等待足够的时间
            Thread.Sleep(400);
            
            // 验证依赖关系是否正确
            AssertTrue(task1.Dependents.Contains(task2), "Task1 should have task2 as dependent");
            AssertTrue(task2.Dependencies.Contains(task1), "Task2 should have task1 as dependency");
        }

        /// <summary>
        /// 测试取消任务
        /// </summary>
        private void TestCancelTask()
        {
            bool executed = false;
            ITask task = _taskScheduler.CreateTask(() => executed = true);
            string taskId = task.Id;
            
            _taskScheduler.SubmitTask(task);
            bool canceled = _taskScheduler.CancelTask(taskId);
            
            AssertTrue(canceled, "Task should be canceled");
            AssertEqual(TaskStatus.Canceled, task.Status, "Task status should be Canceled");
        }

        /// <summary>
        /// 测试清空所有任务
        /// </summary>
        private void TestClearAllTasks()
        {
            // 创建多个任务
            for (int i = 0; i < 5; i++)
            {
                ITask task = _taskScheduler.CreateTask(() => { });
                _taskScheduler.SubmitTask(task);
            }
            
            // 清空所有任务
            _taskScheduler.ClearAllTasks();
            
            // 验证任务是否被清空
            var tasks = _taskScheduler.GetAllTasks();
            AssertEqual(0, tasks.Count, "All tasks should be cleared");
        }

        /// <summary>
        /// 测试获取任务统计信息
        /// </summary>
        private void TestGetStatistics()
        {
            // 创建并提交任务
            ITask task1 = _taskScheduler.CreateTask(() => { });
            ITask task2 = _taskScheduler.CreateTask(() => { });
            
            _taskScheduler.SubmitTask(task1);
            _taskScheduler.SubmitTask(task2);
            
            // 获取统计信息
            TaskStatistics stats = _taskScheduler.GetStatistics();
            
            AssertNotNull(stats, "Statistics should be returned");
            AssertTrue(stats.TotalCount >= 2, "Total task count should be at least 2");
        }
    }
}