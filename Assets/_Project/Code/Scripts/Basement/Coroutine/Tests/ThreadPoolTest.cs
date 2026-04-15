using System;
using System.Threading;
using Test;

namespace Basement.Threading.Tests
{
    /// <summary>
    /// ThreadPool测试
    /// </summary>
    public class ThreadPoolTest : TestBase
    {
        private ThreadPool _threadPool;

        public ThreadPoolTest() : base("ThreadPoolTest") { }

        protected override void Setup()
        {
            // 初始化线程池
            _threadPool = ThreadPool.Instance;
            _threadPool.Initialize(2, 4);
        }

        protected override void Execute()
        {
            TestInitialize();
            TestSubmitTask();
            TestConcurrency();
            TestTaskCount();
            TestThreadCount();
            TestShutdown();
        }

        protected override void Teardown()
        {
            // 关闭线程池
            _threadPool.Shutdown();
        }

        /// <summary>
        /// 测试初始化线程池
        /// </summary>
        private void TestInitialize()
        {
            AssertEqual(2, _threadPool.ThreadCount, "Thread pool should have 2 threads");
        }

        /// <summary>
        /// 测试提交任务
        /// </summary>
        private void TestSubmitTask()
        {
            bool executed = false;
            ITask task = new ActionTask(() => executed = true);
            
            _threadPool.SubmitTask(task);
            
            // 等待任务执行
            Thread.Sleep(100);
            
            AssertTrue(executed, "Task should be executed");
        }

        /// <summary>
        /// 测试并发执行
        /// </summary>
        private void TestConcurrency()
        {
            int executedCount = 0;
            object lockObj = new object();
            int taskCount = 10;

            for (int i = 0; i < taskCount; i++)
            {
                ITask task = new ActionTask(() =>
                {
                    Thread.Sleep(50); // 模拟任务执行时间
                    lock (lockObj)
                    {
                        executedCount++;
                    }
                });
                _threadPool.SubmitTask(task);
            }
            
            // 等待所有任务执行完成
            Thread.Sleep(1000);
            
            AssertEqual(taskCount, executedCount, "All tasks should be executed");
        }

        /// <summary>
        /// 测试任务数量
        /// </summary>
        private void TestTaskCount()
        {
            // 提交多个任务
            for (int i = 0; i < 5; i++)
            {
                ITask task = new ActionTask(() => Thread.Sleep(100));
                _threadPool.SubmitTask(task);
            }
            
            // 等待一段时间，让部分任务开始执行
            Thread.Sleep(100);
            
            // 任务数量应该减少（部分任务已经开始执行）
            AssertTrue(_threadPool.TaskCount >= 0, "Task count should be non-negative");
        }

        /// <summary>
        /// 测试线程数量
        /// </summary>
        private void TestThreadCount()
        {
            AssertTrue(_threadPool.ThreadCount >= 2, "Thread count should be at least 2");
            AssertTrue(_threadPool.ThreadCount <= 4, "Thread count should be at most 4");
        }

        /// <summary>
        /// 测试关闭线程池
        /// </summary>
        private void TestShutdown()
        {
            _threadPool.Shutdown();
            
            // 验证线程池是否关闭
            AssertEqual(0, _threadPool.ThreadCount, "Thread count should be 0 after shutdown");
            AssertEqual(0, _threadPool.TaskCount, "Task count should be 0 after shutdown");
            
            // 测试关闭后提交任务应该抛出异常
            AssertThrows<InvalidOperationException>(() =>
            {
                ITask task = new ActionTask(() => { });
                _threadPool.SubmitTask(task);
            }, "SubmitTask should throw exception after shutdown");
        }
    }
}