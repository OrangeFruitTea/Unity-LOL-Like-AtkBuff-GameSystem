using System.IO;
using UnityEngine;
using Basement.Logging;

namespace Adapter.Bridge
{
    public static class LogConfigProvider
    {
        private static readonly string ConfigPath = Path.Combine(Application.streamingAssetsPath, "LogConfig.json");

        public static LogConfig LoadConfig()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonUtility.FromJson<LogConfig>(json);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"加载日志配置失败: {ex.Message}");
                }
            }

            return GetDefaultConfig();
        }

        public static void SaveConfig(LogConfig config)
        {
            try
            {
                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(ConfigPath, json);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"保存日志配置失败: {ex.Message}");
            }
        }

        public static LogConfig GetDefaultConfig()
        {
            return new LogConfig
            {
                DefaultLogLevel = LogLevel.Info,
                EnableConsoleOutput = true,
                EnableFileOutput = true,
                EnableDebugWindow = true,
                DebugWindowMaxLines = 1000,
                EnableTimestamp = true,
                EnableStackTrace = false,
                MaxLogFileSize = 10
            };
        }
    }
}
