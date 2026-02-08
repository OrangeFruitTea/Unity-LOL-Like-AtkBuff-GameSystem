using System;
using Basement.Logging;
using Adapter.Interfaces;

namespace Adapter.Bridge
{
    public class LogBridge : ILogAdapter
    {
        private static volatile LogBridge _instance;
        private static readonly object _lock = new object();

        public static LogBridge Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LogBridge();
                        }
                    }
                }
                return _instance;
            }
        }

        private LogManager _logManager;

        private LogBridge()
        {
            Initialize();
        }

        private void Initialize()
        {
            var logManagerObj = new UnityEngine.GameObject("[LogManager]");
            _logManager = logManagerObj.AddComponent<LogManager>();
            UnityEngine.Object.DontDestroyOnLoad(logManagerObj);

            var config = new Basement.Logging.LogConfig
            {
                DefaultLogLevel = Basement.Logging.LogLevel.Info,
                EnableConsoleOutput = true,
                EnableFileOutput = true,
                EnableDebugWindow = true
            };

            _logManager.Initialize(config);
        }

        public void Debug(string message, string tag = null)
        {
            _logManager?.LogDebug(message, tag);
        }

        public void Info(string message, string tag = null)
        {
            _logManager?.LogInfo(message, tag);
        }

        public void Warning(string message, string tag = null)
        {
            _logManager?.LogWarning(message, tag);
        }

        public void Error(string message, string tag = null)
        {
            _logManager?.LogError(message, tag);
        }

        public void Fatal(string message, string tag = null)
        {
            _logManager?.LogFatal(message, tag);
        }

        public void Exception(Exception exception, string tag = null)
        {
            _logManager?.LogException(exception, tag);
        }

        public void SetLogLevel(Adapter.LogLevel level)
        {
            _logManager?.SetLogLevel((Basement.Logging.LogLevel)((int)level));
        }

        public Adapter.LogLevel GetLogLevel()
        {
            return _logManager == null ? Adapter.LogLevel.Info : (Adapter.LogLevel)((int)_logManager.CurrentLevel);
        }
    }
}
