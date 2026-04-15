using System;
using System.Diagnostics;

namespace Test
{
    /// <summary>
    /// 测试基类
    /// 提供测试的基本结构和断言机制
    /// </summary>
    public abstract class TestBase
    {
        /// <summary>
        /// 测试名称
        /// </summary>
        public string TestName { get; }

        /// <summary>
        /// 测试结果
        /// </summary>
        public TestResult Result { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="testName">测试名称</param>
        protected TestBase(string testName)
        {
            TestName = testName;
        }

        /// <summary>
        /// 运行测试
        /// </summary>
        /// <returns>测试结果</returns>
        public TestResult Run()
        {
            Result = new TestResult(TestName);
            Stopwatch stopwatch = new Stopwatch();

            try
            {
                stopwatch.Start();
                // 测试前设置
                Setup();
                // 执行测试逻辑
                Execute();
                // 测试后清理
                Teardown();
                stopwatch.Stop();

                // 测试通过
                Result.Passed = true;
                Result.ExecutionTime = stopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // 测试失败
                Result.Passed = false;
                Result.ExecutionTime = stopwatch.ElapsedMilliseconds;
                Result.ErrorMessage = ex.ToString();
            }

            return Result;
        }

        /// <summary>
        /// 测试设置
        /// 子类可以重写此方法进行测试前的初始化操作
        /// </summary>
        protected virtual void Setup() { }

        /// <summary>
        /// 执行测试
        /// 子类必须实现此方法，包含具体的测试逻辑
        /// </summary>
        protected abstract void Execute();

        /// <summary>
        /// 测试清理
        /// 子类可以重写此方法进行测试后的清理操作
        /// </summary>
        protected virtual void Teardown() { }

        /// <summary>
        /// 断言相等
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="expected">预期值</param>
        /// <param name="actual">实际值</param>
        /// <param name="message">错误信息</param>
        protected void AssertEqual<T>(T expected, T actual, string message = null)
        {
            if (!Equals(expected, actual))
            {
                string errorMessage = message ?? $"AssertEqual failed: expected {expected}, actual {actual}";
                throw new AssertionException(errorMessage);
            }
        }

        /// <summary>
        /// 断言为真
        /// </summary>
        /// <param name="condition">条件</param>
        /// <param name="message">错误信息</param>
        protected void AssertTrue(bool condition, string message = null)
        {
            if (!condition)
            {
                string errorMessage = message ?? "AssertTrue failed: condition is false";
                throw new AssertionException(errorMessage);
            }
        }

        /// <summary>
        /// 断言为假
        /// </summary>
        /// <param name="condition">条件</param>
        /// <param name="message">错误信息</param>
        protected void AssertFalse(bool condition, string message = null)
        {
            if (condition)
            {
                string errorMessage = message ?? "AssertFalse failed: condition is true";
                throw new AssertionException(errorMessage);
            }
        }

        /// <summary>
        /// 断言不为空
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="message">错误信息</param>
        protected void AssertNotNull(object value, string message = null)
        {
            if (value == null)
            {
                string errorMessage = message ?? "AssertNotNull failed: value is null";
                throw new AssertionException(errorMessage);
            }
        }

        /// <summary>
        /// 断言为空
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="message">错误信息</param>
        protected void AssertNull(object value, string message = null)
        {
            if (value != null)
            {
                string errorMessage = message ?? "AssertNull failed: value is not null";
                throw new AssertionException(errorMessage);
            }
        }

        /// <summary>
        /// 断言抛出异常
        /// </summary>
        /// <typeparam name="TException">异常类型</typeparam>
        /// <param name="action">要执行的操作</param>
        /// <param name="message">错误信息</param>
        protected void AssertThrows<TException>(Action action, string message = null) where TException : Exception
        {
            try
            {
                action();
                string errorMessage = message ?? $"AssertThrows failed: {typeof(TException).Name} was not thrown";
                throw new AssertionException(errorMessage);
            }
            catch (TException)
            {
                // 预期的异常，测试通过
            }
            catch (Exception ex)
            {
                string errorMessage = message ?? $"AssertThrows failed: expected {typeof(TException).Name}, but got {ex.GetType().Name}";
                throw new AssertionException(errorMessage);
            }
        }
    }

    /// <summary>
    /// 断言异常
    /// 用于表示断言失败时的异常
    /// </summary>
    public class AssertionException : Exception
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误信息</param>
        public AssertionException(string message) : base(message) { }
    }

    /// <summary>
    /// 测试结果
    /// 用于存储测试的执行结果
    /// </summary>
    public class TestResult
    {
        /// <summary>
        /// 测试名称
        /// </summary>
        public string TestName { get; }

        /// <summary>
        /// 是否通过
        /// </summary>
        public bool Passed { get; set; }

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public long ExecutionTime { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="testName">测试名称</param>
        public TestResult(string testName)
        {
            TestName = testName;
            Passed = false;
            ExecutionTime = 0;
            ErrorMessage = null;
        }

        /// <summary>
        /// 输出结果
        /// </summary>
        /// <returns>结果字符串</returns>
        public override string ToString()
        {
            string status = Passed ? "PASS" : "FAIL";
            string result = $"[{status}] {TestName} - {ExecutionTime}ms";
            if (!Passed && !string.IsNullOrEmpty(ErrorMessage))
            {
                result += $"\nError: {ErrorMessage}";
            }
            return result;
        }
    }
}