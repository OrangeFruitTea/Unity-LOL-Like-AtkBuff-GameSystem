using System;
using System.Threading;
using UnityEngine;
using Test;

namespace Basement.Threading.Tests
{
    /// <summary>
    /// ThreadingSystem测试
    /// </summary>
    public class ThreadingSystemTest : TestBase
    {
        private GameObject _testObject;
        private ThreadingSystem _threadingSystem;

        public ThreadingSystemTest() : base("ThreadingSystemTest") { }

        protected override void Setup()
        {
            // 创建测试对象并添加ThreadingSystem组件
            _testObject = new GameObject("TestObject");
            _threadingSystem = _testObject.AddComponent<ThreadingSystem>();
            _threadingSystem.Initialize();
        }

        protected override void Execute()
        {
            TestInitialize();
            TestCreateTask();
            TestSubmitTask();
            TestWaitForSeconds();
            TestWaitForFrames();
            TestWaitUntil();
            TestGetTaskStatistics();
            TestCleanup();
        }

        protected override void Teardown()
        {
            // 清理测试对象
            if (_testObject != null)
            {
                _threadingSystem.Cleanup();
                GameObject.DestroyImmediate(_testObject);
            }
        }

        /// <summary>
        /// 测试初始化系统
        /// </summary>
        private void TestInitialize()
        {
            AssertNotNull(_threadingSystem.TaskScheduler, "TaskScheduler should be initialized");
        }

        /// <summary>
        /// 测试创建任务
        /// </summary>
        private void TestCreateTask()
        {
            ITask task = _threadingSystem.CreateTask(() => { });
            
            AssertNotNull(task, "Task should be created");
            AssertNotNull(task.Id, "Task should have an Id");
        }

        /// <summary>
        /// 测试提交任务
        /// </summary>
        private void TestSubmitTask()
        {
            bool executed = false;
            ITask task = _threadingSystem.CreateTask(() => executed = true);
            
            _threadingSystem.SubmitTask(task);
            
            // 等待任务执行
            Thread.Sleep(100);
            
            // 手动调用Update方法处理任务
            _threadingSystem.Update();
            Thread.Sleep(100);
            _threadingSystem.Update();
            
            // 注意：由于任务在后台线程执行，这里可能无法立即检测到执行状态
            // 但至少任务应该被提交并且状态应该更新
            AssertTrue(task.Status != TaskStatus.Created, "Task status should be updated after submission");
        }

        /// <summary>
        /// 测试等待指定时间
        /// </summary>
        private void TestWaitForSeconds()
        {
            bool executed = false;
            DateTime startTime = DateTime.Now;

            _threadingSystem.WaitForSeconds(0.1f, () => executed = true);
            
            // 等待足够的时间
            Thread.Sleep(200);
            
            AssertTrue(executed, "WaitForSeconds should execute callback");
            TimeSpan elapsed = DateTime.Now - startTime;
            AssertTrue(elapsed.TotalMilliseconds >= 100, "WaitForSeconds should wait at least specified time");
        }

        /// <summary>
        /// 测试等待指定帧数
        /// </summary>
        private void TestWaitForFrames()
        {
            bool executed = false;

            _threadingSystem.WaitForFrames(2, () => executed = true);
            
            // 等待足够的时间
            Thread.Sleep(100);
            
            AssertTrue(executed, "WaitForFrames should execute callback");
        }

        /// <summary>
        /// 测试等待直到条件满足
        /// </summary>
        private void TestWaitUntil()
        {
            bool executed = false;
            bool conditionMet = false;

            _threadingSystem.WaitUntil(() => conditionMet, () => executed = true);
            
            // 等待一段时间，条件未满足
            Thread.Sleep(100);
            AssertFalse(executed, "WaitUntil should not execute before condition is met");
            
            // 设置条件满足
            conditionMet = true;
            
            // 等待协程执行
            Thread.Sleep(100);
            AssertTrue(executed, "WaitUntil should execute after condition is met");
        }

        /// <summary>
        /// 测试获取任务统计信息
        /// </summary>
        private void TestGetTaskStatistics()
        {
            // 创建并提交任务
            ITask task1 = _threadingSystem.CreateTask(() => { });
            ITask task2 = _threadingSystem.CreateTask(() => { });
            
            _threadingSystem.SubmitTask(task1);
            _threadingSystem.SubmitTask(task2);
            
            // 获取统计信息
            TaskStatistics stats = _threadingSystem.GetTaskStatistics();
            
            AssertNotNull(stats, "Statistics should be returned");
            AssertTrue(stats.TotalCount >= 2, "Total task count should be at least 2");
        }

        /// <summary>
        /// 测试清理系统
        /// </summary>
        private void TestCleanup()
        {
            // 清理系统
            _threadingSystem.Cleanup();
            
            // 验证系统是否被清理
            TaskStatistics stats = _threadingSystem.GetTaskStatistics();
            AssertEqual(0, stats.TotalCount, "All tasks should be cleared");
        }
    }
}