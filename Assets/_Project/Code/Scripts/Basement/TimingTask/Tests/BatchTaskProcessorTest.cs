using System;
using Test;

namespace Basement.Tasks.Tests
{
    /// <summary>
    /// 批量任务处理器测试
    /// 测试BatchTaskProcessor的基本功能和边界条件
    /// </summary>
    public class BatchTaskProcessorTest : TestBase
    {
        private BatchTaskProcessor _processor;

        public BatchTaskProcessorTest() : base("BatchTaskProcessorTest") { }

        protected override void Setup()
        {
            _processor = new BatchTaskProcessor();
        }

        protected override void Execute()
        {
            TestAddTask();
            TestBatchSize();
            TestBatchInterval();
            TestForceProcess();
            TestBatchCount();
            TestMultipleTasks();
            TestSetBatchSize();
            TestSetBatchInterval();
            TestNullTask();
            TestTaskExecution();
        }

        /// <summary>
        /// 测试添加任务
        /// </summary>
        private void TestAddTask()
        {
            AssertEqual(0, _processor.BatchCount, "初始批处理数量应为0");

            bool executed = false;
            TimeTriggeredTask task = new TimeTriggeredTask(() => executed = true, 0f);
            _processor.AddTask(task);

            AssertEqual(1, _processor.BatchCount, "添加任务后批处理数量应为1");
        }

        /// <summary>
        /// 测试批处理大小
        /// </summary>
        private void TestBatchSize()
        {
            _processor.SetBatchSize(10);

            for (int i = 0; i < 9; i++)
            {
                _processor.AddTask(new TimeTriggeredTask(() => { }, 0f));
            }

            AssertEqual(9, _processor.BatchCount, "添加9个任务后批处理数量应为9");

            _processor.AddTask(new TimeTriggeredTask(() => { }, 0f));

            AssertEqual(0, _processor.BatchCount, "达到批处理大小后批处理数量应为0");
        }

        /// <summary>
        /// 测试批处理间隔
        /// </summary>
        private void TestBatchInterval()
        {
            _processor.SetBatchInterval(0.1f);
            _processor.SetBatchSize(100);

            _processor.AddTask(new TimeTriggeredTask(() => { }, 0f));

            AssertEqual(1, _processor.BatchCount, "添加任务后批处理数量应为1");

            System.Threading.Thread.Sleep(150);

            _processor.AddTask(new TimeTriggeredTask(() => { }, 0f));

            AssertEqual(0, _processor.BatchCount, "超过批处理间隔后批处理数量应为0");
        }

        /// <summary>
        /// 测试强制处理
        /// </summary>
        private void TestForceProcess()
        {
            _processor.SetBatchSize(100);

            for (int i = 0; i < 5; i++)
            {
                _processor.AddTask(new TimeTriggeredTask(() => { }, 0f));
            }

            AssertEqual(5, _processor.BatchCount, "添加5个任务后批处理数量应为5");

            _processor.ForceProcess();

            AssertEqual(0, _processor.BatchCount, "强制处理后批处理数量应为0");
        }

        /// <summary>
        /// 测试批处理数量
        /// </summary>
        private void TestBatchCount()
        {
            AssertEqual(0, _processor.BatchCount, "初始批处理数量应为0");

            _processor.AddTask(new TimeTriggeredTask(() => { }, 0f));
            _processor.AddTask(new TimeTriggeredTask(() => { }, 0f));
            _processor.AddTask(new TimeTriggeredTask(() => { }, 0f));

            AssertEqual(3, _processor.BatchCount, "添加3个任务后批处理数量应为3");
        }

        /// <summary>
        /// 测试多个任务
        /// </summary>
        private void TestMultipleTasks()
        {
            int[] counters = new int[10];

            for (int i = 0; i < 10; i++)
            {
                int index = i;
                _processor.AddTask(new TimeTriggeredTask(() => counters[index]++, 0f));
            }

            _processor.ForceProcess();

            AssertEqual(0, _processor.BatchCount, "处理后批处理数量应为0");

            for (int i = 0; i < 10; i++)
            {
                AssertEqual(1, counters[i], $"第{i}个任务应执行一次");
            }
        }

        /// <summary>
        /// 测试设置批处理大小
        /// </summary>
        private void TestSetBatchSize()
        {
            _processor.SetBatchSize(5);

            for (int i = 0; i < 4; i++)
            {
                _processor.AddTask(new TimeTriggeredTask(() => { }, 0f));
            }

            AssertEqual(4, _processor.BatchCount, "添加4个任务后批处理数量应为4");

            _processor.AddTask(new TimeTriggeredTask(() => { }, 0f));

            AssertEqual(0, _processor.BatchCount, "达到批处理大小后批处理数量应为0");
        }

        /// <summary>
        /// 测试设置批处理间隔
        /// </summary>
        private void TestSetBatchInterval()
        {
            _processor.SetBatchInterval(0.05f);
            _processor.SetBatchSize(100);

            _processor.AddTask(new TimeTriggeredTask(() => { }, 0f));

            AssertEqual(1, _processor.BatchCount, "添加任务后批处理数量应为1");

            System.Threading.Thread.Sleep(100);

            _processor.AddTask(new TimeTriggeredTask(() => { }, 0f));

            AssertEqual(0, _processor.BatchCount, "超过批处理间隔后批处理数量应为0");
        }

        /// <summary>
        /// 测试空任务
        /// </summary>
        private void TestNullTask()
        {
            int countBefore = _processor.BatchCount;

            _processor.AddTask(null);

            AssertEqual(countBefore, _processor.BatchCount, "添加null任务不应改变批处理数量");
        }

        /// <summary>
        /// 测试任务执行
        /// </summary>
        private void TestTaskExecution()
        {
            bool[] executed = new bool[5];

            for (int i = 0; i < 5; i++)
            {
                int index = i;
                _processor.AddTask(new TimeTriggeredTask(() => executed[index] = true, 0f));
            }

            _processor.ForceProcess();

            for (int i = 0; i < 5; i++)
            {
                AssertTrue(executed[i], $"第{i}个任务应被执行");
            }
        }
    }
}
