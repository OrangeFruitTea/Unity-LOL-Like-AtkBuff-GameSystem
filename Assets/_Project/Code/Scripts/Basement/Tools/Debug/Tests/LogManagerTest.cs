using System;
using System.Collections;
using UnityEngine;
using Basement.Logging;

namespace Basement.Tools.Debug.Tests
{
    public class LogManagerTest : MonoBehaviour
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

            if (Input.GetKeyDown(KeyCode.F7))
            {
                TestDebugWindow();
            }
        }

        private IEnumerator RunAllTests()
        {
            Debug.Log("=== 开始LogManager完整测试 ===");

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

            TestDebugWindow();
            yield return new WaitForSeconds(testDelay);

            LogManager.Instance.LogInfo("=== LogManager测试完成 ===", "LogManagerTest");
        }

        private void TestLogLevels()
        {
            LogManager.Instance.LogInfo("=== 测试不同日志级别 ===", "LogManagerTest");

            for (int i = 0; i < logCountPerLevel; i++)
            {
                LogManager.Instance.LogDebug($"这是Debug日志 #{i + 1}", "TestDebug");
                LogManager.Instance.LogInfo($"这是Info日志 #{i + 1}", "TestInfo");
                LogManager.Instance.LogWarning($"这是Warning日志 #{i + 1}", "TestWarning");
                LogManager.Instance.LogError($"这是Error日志 #{i + 1}", "TestError");
                LogManager.Instance.LogFatal($"这是Fatal日志 #{i + 1}", "TestFatal");
            }

            LogManager.Instance.LogInfo("日志级别测试完成", "LogManagerTest");
        }

        private void TestLogWithTags()
        {
            LogManager.Instance.LogInfo("=== 测试带标签的日志 ===", "LogManagerTest");

            LogManager.Instance.LogInfo("网络系统日志", "Network");
            LogManager.Instance.LogInfo("玩家系统日志", "Player");
            LogManager.Instance.LogInfo("Buff系统日志", "Buff");
            LogManager.Instance.LogWarning("网络连接不稳定", "Network");
            LogManager.Instance.LogWarning("玩家移动异常", "Player");
            LogManager.Instance.LogWarning("Buff叠加失败", "Buff");
            LogManager.Instance.LogError("网络连接断开", "Network");
            LogManager.Instance.LogError("玩家数据损坏", "Player");
            LogManager.Instance.LogError("Buff配置错误", "Buff");

            LogManager.Instance.LogInfo("带标签日志测试完成", "LogManagerTest");
        }

        private void TestExceptionLogging()
        {
            LogManager.Instance.LogInfo("=== 测试异常日志 ===", "LogManagerTest");

            try
            {
                throw new InvalidOperationException("测试异常1");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogException(ex, "TestException");
            }

            try
            {
                throw new ArgumentNullException("testParam", "测试异常2");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogException(ex, "TestException");
            }

            try
            {
                int[] array = new int[5];
                int value = array[10];
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogException(ex, "TestException");
            }

            LogManager.Instance.LogInfo("异常日志测试完成", "LogManagerTest");
        }

        private void TestLogLevelFiltering()
        {
            LogManager.Instance.LogInfo("=== 测试日志级别过滤 ===", "LogManagerTest");

            LogManager.Instance.LogInfo("当前日志级别: " + LogManager.Instance.CurrentLevel, "LogManagerTest");

            LogManager.Instance.LogDebug("这条Debug日志应该显示", "FilterTest");
            LogManager.Instance.LogInfo("这条Info日志应该显示", "FilterTest");
            LogManager.Instance.LogWarning("这条Warning日志应该显示", "FilterTest");
            LogManager.Instance.LogError("这条Error日志应该显示", "FilterTest");

            LogManager.Instance.LogInfo("设置日志级别为Warning", "LogManagerTest");
            LogManager.Instance.SetLogLevel(LogLevel.Warning);

            LogManager.Instance.LogDebug("这条Debug日志不应该显示", "FilterTest");
            LogManager.Instance.LogInfo("这条Info日志不应该显示", "FilterTest");
            LogManager.Instance.LogWarning("这条Warning日志应该显示", "FilterTest");
            LogManager.Instance.LogError("这条Error日志应该显示", "FilterTest");

            LogManager.Instance.LogInfo("恢复日志级别为Info", "LogManagerTest");
            LogManager.Instance.SetLogLevel(LogLevel.Info);

            LogManager.Instance.LogDebug("这条Debug日志不应该显示", "FilterTest");
            LogManager.Instance.LogInfo("这条Info日志应该显示", "FilterTest");
            LogManager.Instance.LogWarning("这条Warning日志应该显示", "FilterTest");
            LogManager.Instance.LogError("这条Error日志应该显示", "FilterTest");

            LogManager.Instance.LogInfo("日志级别过滤测试完成", "LogManagerTest");
        }

        private void TestPerformance()
        {
            LogManager.Instance.LogInfo("=== 测试性能 ===", "LogManagerTest");

            int testCount = 1000;
            float startTime = Time.realtimeSinceStartup;

            for (int i = 0; i < testCount; i++)
            {
                LogManager.Instance.LogInfo($"性能测试日志 #{i}", "Performance");
            }

            float endTime = Time.realtimeSinceStartup;
            float duration = endTime - startTime;
            float logsPerSecond = testCount / duration;

            LogManager.Instance.LogInfo($"性能测试完成: {testCount}条日志，耗时{duration:F3}秒，平均{logsPerSecond:F0}条/秒", "LogManagerTest");
        }

        private void TestDebugWindow()
        {
            LogManager.Instance.LogInfo("=== 测试DebugWindow ===", "LogManagerTest");
            LogManager.Instance.LogInfo("按F12键可以打开/关闭DebugWindow", "LogManagerTest");
            LogManager.Instance.LogInfo("DebugWindow支持搜索、过滤、自动滚动等功能", "LogManagerTest");

            DebugWindow.Instance.ShowWindow();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));

            GUIStyle boldLabel = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14
            };

            GUILayout.Label("LogManager测试工具", boldLabel);
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

            if (GUILayout.Button("测试DebugWindow (F7)"))
            {
                TestDebugWindow();
            }

            GUILayout.Space(10);
            GUILayout.Label($"当前日志级别: {LogManager.Instance.CurrentLevel}");

            GUILayout.EndArea();
        }
    }
}
