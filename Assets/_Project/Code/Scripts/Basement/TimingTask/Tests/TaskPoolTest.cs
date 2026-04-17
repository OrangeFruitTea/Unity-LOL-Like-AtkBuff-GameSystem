using Test;

namespace Basement.Tasks.Tests
{
    /// <summary>
    /// 任务池测试
    /// 测试TaskPool的基本功能和边界条件
    /// </summary>
    public class TaskPoolTest : TestBase
    {
        private TaskPool<TestTask> _pool;

        public TaskPoolTest() : base("TaskPoolTest") { }

        protected override void Setup()
        {
            _pool = new TaskPool<TestTask>();
        }

        protected override void Teardown()
        {
            _pool.Clear();
        }

        protected override void Execute()
        {
            TestGetFromEmptyPool();
            TestGetAndReturn();
            TestMultipleGetsAndReturns();
            TestPoolCount();
            TestClearPool();
            TestReturnNull();
            TestPoolReusability();
            TestPoolCapacity();
        }

        /// <summary>
        /// 测试从空池获取任务
        /// </summary>
        private void TestGetFromEmptyPool()
        {
            AssertEqual(0, _pool.Count, "空池数量应为0");

            TestTask task = _pool.Get();

            AssertNotNull(task, "应能从空池获取任务");
            AssertEqual(0, _pool.Count, "空池获取后数量仍为0");
        }

        /// <summary>
        /// 测试获取和返回任务
        /// </summary>
        private void TestGetAndReturn()
        {
            TestTask task1 = _pool.Get();
            TestTask task2 = _pool.Get();

            AssertEqual(0, _pool.Count, "获取两个任务后池数量应为0");

            _pool.Return(task1);

            AssertEqual(1, _pool.Count, "返回一个任务后池数量应为1");

            _pool.Return(task2);

            AssertEqual(2, _pool.Count, "返回两个任务后池数量应为2");
        }

        /// <summary>
        /// 测试多次获取和返回
        /// </summary>
        private void TestMultipleGetsAndReturns()
        {
            TestTask[] tasks = new TestTask[10];

            for (int i = 0; i < 10; i++)
            {
                tasks[i] = _pool.Get();
            }

            AssertEqual(0, _pool.Count, "获取10个任务后池数量应为0");

            for (int i = 0; i < 10; i++)
            {
                _pool.Return(tasks[i]);
            }

            AssertEqual(10, _pool.Count, "返回10个任务后池数量应为10");

            for (int i = 0; i < 10; i++)
            {
                TestTask task = _pool.Get();
                AssertNotNull(task, "应能获取返回的任务");
            }

            AssertEqual(0, _pool.Count, "再次获取10个任务后池数量应为0");
        }

        /// <summary>
        /// 测试池数量
        /// </summary>
        private void TestPoolCount()
        {
            AssertEqual(0, _pool.Count, "初始池数量应为0");

            _pool.Return(_pool.Get());
            _pool.Return(_pool.Get());
            _pool.Return(_pool.Get());

            AssertEqual(3, _pool.Count, "返回3个任务后池数量应为3");

            _pool.Get();
            _pool.Get();

            AssertEqual(1, _pool.Count, "获取2个任务后池数量应为1");
        }

        /// <summary>
        /// 测试清空池
        /// </summary>
        private void TestClearPool()
        {
            _pool.Return(_pool.Get());
            _pool.Return(_pool.Get());
            _pool.Return(_pool.Get());

            AssertEqual(3, _pool.Count, "返回3个任务后池数量应为3");

            _pool.Clear();

            AssertEqual(0, _pool.Count, "清空后池数量应为0");
        }

        /// <summary>
        /// 测试返回null任务
        /// </summary>
        private void TestReturnNull()
        {
            int countBefore = _pool.Count;

            _pool.Return(null);

            AssertEqual(countBefore, _pool.Count, "返回null不应改变池数量");
        }

        /// <summary>
        /// 测试池的可重用性
        /// </summary>
        private void TestPoolReusability()
        {
            TestTask task1 = _pool.Get();
            task1.Value = 100;

            _pool.Return(task1);

            TestTask task2 = _pool.Get();

            AssertEqual(task1, task2, "应获取到同一个任务对象");
            AssertEqual(100, task2.Value, "任务对象的状态应保持");
        }

        /// <summary>
        /// 测试池容量
        /// </summary>
        private void TestPoolCapacity()
        {
            for (int i = 0; i < 100; i++)
            {
                _pool.Return(_pool.Get());
            }

            AssertEqual(100, _pool.Count, "池应能容纳100个任务");

            for (int i = 0; i < 100; i++)
            {
                _pool.Get();
            }

            AssertEqual(0, _pool.Count, "获取100个任务后池数量应为0");
        }
    }

    /// <summary>
    /// 测试任务类
    /// 用于TaskPool测试
    /// </summary>
    public class TestTask : ITimingTask
    {
        public string TaskId { get; set; }
        public TimingTaskState State { get; set; }
        public TimingTaskPriority Priority { get; set; }
        public float DelayTime { get; set; }

        public int Value { get; set; }

        public TestTask()
        {
            TaskId = System.Guid.NewGuid().ToString();
            State = TimingTaskState.Ready;
            Priority = TimingTaskPriority.Normal;
            DelayTime = 0f;
            Value = 0;
        }

        public void Execute()
        {
            State = TimingTaskState.Completed;
        }

        public void Cancel()
        {
            State = TimingTaskState.Completed;
        }
    }
}
