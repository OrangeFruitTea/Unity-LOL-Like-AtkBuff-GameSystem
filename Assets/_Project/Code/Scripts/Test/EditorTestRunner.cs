using UnityEditor;
using UnityEngine;

namespace Test
{
    /// <summary>
    /// Unity编辑器测试运行器
    /// 用于在Unity编辑器中运行测试用例
    /// </summary>
    public class EditorTestRunner
    {
        /// <summary>
        /// 运行协程模块测试
        /// 通过Unity编辑器菜单调用
        /// </summary>
        [MenuItem("Tests/Run Coroutine Tests")]
        public static void RunCoroutineTests()
        {
            TestRunner runner = new TestRunner();

            // 添加协程模块的测试用例
            runner.AddTest(new Basement.Threading.Tests.CoroutineManagerTest());
            runner.AddTest(new Basement.Threading.Tests.TaskSchedulerTest());
            runner.AddTest(new Basement.Threading.Tests.ThreadPoolTest());
            runner.AddTest(new Basement.Threading.Tests.ThreadingSystemTest());

            // 运行所有测试
            TestSummary summary = runner.RunAll();

            // 在Unity控制台输出测试结果
            Debug.Log(summary.ToString());
        }

        /// <summary>
        /// 运行资源池模块测试
        /// 通过Unity编辑器菜单调用
        /// </summary>
        [MenuItem("Tests/Run Resource Pool Tests")]
        public static void RunResourcePoolTests()
        {
            TestRunner runner = new TestRunner();

            // 添加资源池模块的测试用例
            runner.AddTest(new Basement.ResourceManagement.Tests.ResourcePoolManagerTest());
            runner.AddTest(new Basement.ResourceManagement.Tests.GameObjectPoolTest());
            runner.AddTest(new Basement.ResourceManagement.Tests.GenericResourcePoolTest());

            // 运行所有测试
            TestSummary summary = runner.RunAll();

            // 在Unity控制台输出测试结果
            Debug.Log(summary.ToString());
        }

        /// <summary>
        /// 运行时序任务模块测试
        /// 通过Unity编辑器菜单调用
        /// </summary>
        [MenuItem("Tests/Run Timing Task Tests")]
        public static void RunTimingTaskTests()
        {
            TestRunner runner = new TestRunner();

            // 添加时序任务模块的测试用例
            runner.AddTest(new Basement.Tasks.Tests.TimeTriggeredTaskTest());
            runner.AddTest(new Basement.Tasks.Tests.PeriodicTaskTest());
            runner.AddTest(new Basement.Tasks.Tests.ConditionalTaskTest());
            runner.AddTest(new Basement.Tasks.Tests.TimingTaskManagerTest());
            runner.AddTest(new Basement.Tasks.Tests.TimingTaskSchedulerTest());
            runner.AddTest(new Basement.Tasks.Tests.TaskPoolTest());
            runner.AddTest(new Basement.Tasks.Tests.BatchTaskProcessorTest());

            // 运行所有测试
            TestSummary summary = runner.RunAll();

            // 在Unity控制台输出测试结果
            Debug.Log(summary.ToString());
        }
    }
}