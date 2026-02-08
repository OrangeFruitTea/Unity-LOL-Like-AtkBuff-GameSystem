using System.Collections;
using UnityEngine;
using Adapter;
using Adapter.Bridge;

namespace Adapter.Tests
{
    public class LogAdapterTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private float testDelay = 1f;
        [SerializeField] private int logCountPerLevel = 3;

        private void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                StartCoroutine(RunAllTests());
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                TestLogLevels();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                TestLogWithTags();
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                TestExceptionLogging();
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                TestLogLevelFiltering();
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                TestPerformance();
            }
        }

        private IEnumerator RunAllTests()
        {
            Debug.Log("=== 开始LogAdapter完整测试 ===");

            yield return new WaitForSeconds(testDelay);

            TestLogLevels();
            yield return new WaitForSeconds(testDelay);

            TestLogWithTags();
            yield return new WaitForSeconds(testDelay);

            TestExceptionLogging();
            yield return new WaitForSeconds(testDelay);

            TestLogLevelFiltering();
            yield return new WaitForSeconds(testDelay);

            TestPerformance();
            yield return new WaitForSeconds(testDelay);

            LogBridge.Instance.Info("=== LogAdapter测试完成 ===", "LogAdapterTest");
        }

        private void TestLogLevels()
        {
            LogBridge.Instance.Info("=== 测试不同日志级别 ===", "LogAdapterTest");

            for (int i = 0; i < logCountPerLevel; i++)
            {
                LogBridge.Instance.Debug($"这是Debug日志 #{i + 1}", "TestDebug");
                LogBridge.Instance.Info($"这是Info日志 #{i + 1}", "TestInfo");
                LogBridge.Instance.Warning($"这是Warning日志 #{i + 1}", "TestWarning");
                LogBridge.Instance.Error($"这是Error日志 #{i + 1}", "TestError");
                LogBridge.Instance.Fatal($"这是Fatal日志 #{i + 1}", "TestFatal");
            }

            LogBridge.Instance.Info("日志级别测试完成", "LogAdapterTest");
        }

        private void TestLogWithTags()
        {
            LogBridge.Instance.Info("=== 测试带标签的日志 ===", "LogAdapterTest");

            LogBridge.Instance.Info("网络系统日志", "Network");
            LogBridge.Instance.Info("玩家系统日志", "Player");
            LogBridge.Instance.Info("Buff系统日志", "Buff");
            LogBridge.Instance.Warning("网络连接不稳定", "Network");
            LogBridge.Instance.Warning("玩家移动异常", "Player");
            LogBridge.Instance.Warning("Buff叠加失败", "Buff");
            LogBridge.Instance.Error("网络连接断开", "Network");
            LogBridge.Instance.Error("玩家数据损坏", "Player");
            LogBridge.Instance.Error("Buff配置错误", "Buff");

            LogBridge.Instance.Info("带标签日志测试完成", "LogAdapterTest");
        }

        private void TestExceptionLogging()
        {
            LogBridge.Instance.Info("=== 测试异常日志 ===", "LogAdapterTest");

            try
            {
                throw new System.InvalidOperationException("测试异常1");
            }
            catch (System.Exception ex)
            {
                LogBridge.Instance.Exception(ex, "TestException");
            }

            try
            {
                throw new System.ArgumentNullException("testParam", "测试异常2");
            }
            catch (System.Exception ex)
            {
                LogBridge.Instance.Exception(ex, "TestException");
            }

            try
            {
                int[] array = new int[5];
                int value = array[10];
            }
            catch (System.Exception ex)
            {
                LogBridge.Instance.Exception(ex, "TestException");
            }

            LogBridge.Instance.Info("异常日志测试完成", "LogAdapterTest");
        }

        private void TestLogLevelFiltering()
        {
            LogBridge.Instance.Info("=== 测试日志级别过滤 ===", "LogAdapterTest");

            LogBridge.Instance.Info($"当前日志级别: {LogBridge.Instance.GetLogLevel()}", "LogAdapterTest");

            LogBridge.Instance.Debug("这条Debug日志应该显示", "FilterTest");
            LogBridge.Instance.Info("这条Info日志应该显示", "FilterTest");
            LogBridge.Instance.Warning("这条Warning日志应该显示", "FilterTest");
            LogBridge.Instance.Error("这条Error日志应该显示", "FilterTest");

            LogBridge.Instance.Info("设置日志级别为Warning", "LogAdapterTest");
            LogBridge.Instance.SetLogLevel(LogLevel.Warning);

            LogBridge.Instance.Debug("这条Debug日志不应该显示", "FilterTest");
            LogBridge.Instance.Info("这条Info日志不应该显示", "FilterTest");
            LogBridge.Instance.Warning("这条Warning日志应该显示", "FilterTest");
            LogBridge.Instance.Error("这条Error日志应该显示", "FilterTest");

            LogBridge.Instance.Info("恢复日志级别为Info", "LogAdapterTest");
            LogBridge.Instance.SetLogLevel(LogLevel.Info);

            LogBridge.Instance.Debug("这条Debug日志不应该显示", "FilterTest");
            LogBridge.Instance.Info("这条Info日志应该显示", "FilterTest");
            LogBridge.Instance.Warning("这条Warning日志应该显示", "FilterTest");
            LogBridge.Instance.Error("这条Error日志应该显示", "FilterTest");

            LogBridge.Instance.Info("日志级别过滤测试完成", "LogAdapterTest");
        }

        private void TestPerformance()
        {
            LogBridge.Instance.Info("=== 测试性能 ===", "LogAdapterTest");

            int testCount = 1000;
            float startTime = Time.realtimeSinceStartup;

            for (int i = 0; i < testCount; i++)
            {
                LogBridge.Instance.Info($"性能测试日志 #{i}", "Performance");
            }

            float endTime = Time.realtimeSinceStartup;
            float duration = endTime - startTime;
            float logsPerSecond = testCount / duration;

            LogBridge.Instance.Info($"性能测试完成: {testCount}条日志，耗时{duration:F3}秒，平均{logsPerSecond:F0}条/秒", "LogAdapterTest");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));

            GUIStyle boldLabel = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14
            };

            GUILayout.Label("LogAdapter测试工具", boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("运行所有测试 (F1)"))
            {
                StartCoroutine(RunAllTests());
            }

            if (GUILayout.Button("测试日志级别 (F2)"))
            {
                TestLogLevels();
            }

            if (GUILayout.Button("测试带标签日志 (F3)"))
            {
                TestLogWithTags();
            }

            if (GUILayout.Button("测试异常日志 (F4)"))
            {
                TestExceptionLogging();
            }

            if (GUILayout.Button("测试日志过滤 (F5)"))
            {
                TestLogLevelFiltering();
            }

            if (GUILayout.Button("测试性能 (F6)"))
            {
                TestPerformance();
            }

            GUILayout.Space(10);
            GUILayout.Label($"当前日志级别: {LogBridge.Instance.GetLogLevel()}");

            GUILayout.EndArea();
        }
    }
}
