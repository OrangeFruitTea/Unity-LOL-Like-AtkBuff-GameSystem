using System;
using UnityEngine;

namespace Basement.Logging
{
    public class ConsoleOutput : ILogOutput
    {
        private LogLevel _logLevel = LogLevel.Info;
        private bool _enableTimestamp = true;

        public ConsoleOutput(bool enableTimestamp = true)
        {
            _enableTimestamp = enableTimestamp;
        }

        public void Log(string message, LogLevel level, string tag = null)
        {
            if (level < _logLevel)
                return;

            string formattedMessage = FormatMessage(message, level, tag);

            switch (level)
            {
                case LogLevel.Debug:
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log(formattedMessage);
                    #endif
                    break;
                case LogLevel.Info:
                    Debug.Log(formattedMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    Debug.LogError(formattedMessage);
                    break;
            }
        }

        public void SetLogLevel(LogLevel level)
        {
            _logLevel = level;
        }

        private string FormatMessage(string message, LogLevel level, string tag)
        {
            if (!_enableTimestamp)
                return message;

            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            string levelStr = level.ToString().ToUpper();
            string tagStr = string.IsNullOrEmpty(tag) ? "" : $"[{tag}]";
            return $"[{time}] {levelStr} {tagStr} {message}";
        }
    }
}
