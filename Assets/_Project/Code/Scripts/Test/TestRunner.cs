using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Test
{
    /// <summary>
    /// 测试运行器
    /// 用于执行测试用例并输出测试结果报告
    /// </summary>
    public class TestRunner
    {
        private readonly List<TestBase> _tests = new List<TestBase>();

        /// <summary>
        /// 添加测试
        /// </summary>
        /// <param name="test">测试用例</param>
        public void AddTest(TestBase test)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test), "测试用例不能为空");
            }
            _tests.Add(test);
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        /// <returns>测试摘要</returns>
        public TestSummary RunAll()
        {
            TestSummary summary = new TestSummary();
            Stopwatch stopwatch = new Stopwatch();

            // 输出测试开始信息
            Console.WriteLine("========================================");
            Console.WriteLine("开始执行测试...");
            Console.WriteLine($"共 {_tests.Count} 个测试用例");
            Console.WriteLine("========================================");

            stopwatch.Start();

            // 执行所有测试用例
            foreach (var test in _tests)
            {
                TestResult result = test.Run();
                summary.AddResult(result);
                Console.WriteLine(result);
                Console.WriteLine();
            }

            stopwatch.Stop();

            // 输出测试摘要
            Console.WriteLine("========================================");
            Console.WriteLine(summary);
            Console.WriteLine("========================================");

            return summary;
        }
    }

    /// <summary>
    /// 测试摘要
    /// 用于汇总测试结果
    /// </summary>
    public class TestSummary
    {
        private readonly List<TestResult> _results = new List<TestResult>();

        /// <summary>
        /// 总测试数
        /// </summary>
        public int TotalCount => _results.Count;

        /// <summary>
        /// 通过测试数
        /// </summary>
        public int PassedCount => _results.Count(r => r.Passed);

        /// <summary>
        /// 失败测试数
        /// </summary>
        public int FailedCount => _results.Count(r => !r.Passed);

        /// <summary>
        /// 总执行时间（毫秒）
        /// </summary>
        public long TotalExecutionTime => _results.Sum(r => r.ExecutionTime);

        /// <summary>
        /// 添加测试结果
        /// </summary>
        /// <param name="result">测试结果</param>
        public void AddResult(TestResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result), "测试结果不能为空");
            }
            _results.Add(result);
        }

        /// <summary>
        /// 输出摘要
        /// </summary>
        /// <returns>摘要字符串</returns>
        public override string ToString()
        {
            string summary = $"测试完成：\n" +
                            $"总测试数：{TotalCount}\n" +
                            $"通过测试：{PassedCount}\n" +
                            $"失败测试：{FailedCount}\n" +
                            $"总执行时间：{TotalExecutionTime}ms\n" +
                            $"成功率：{(TotalCount > 0 ? (PassedCount * 100.0 / TotalCount) : 0):F2}%";

            if (FailedCount > 0)
            {
                summary += "\n\n失败测试：";
                foreach (var result in _results.Where(r => !r.Passed))
                {
                    summary += $"\n- {result.TestName}";
                }
            }

            return summary;
        }
    }
}