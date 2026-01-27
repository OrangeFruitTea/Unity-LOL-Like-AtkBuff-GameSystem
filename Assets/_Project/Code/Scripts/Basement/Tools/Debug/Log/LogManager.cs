using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Basement.Utils;

namespace Basement.Logging
{
    public class LogManager : Singleton<LogManager>
    {
        private readonly List<ILogOutput> _outputs = new List<ILogOutput>();
        private LogLevel _currentLevel = LogLevel.Info;
        private bool _isInitialized = false;
        private readonly StringBuilder _messageBuilder = new StringBuilder(256);
        private bool _enableTimestamp = true;
        private bool _enableStackTrace = false;

        public LogLevel CurrentLevel => _currentLevel;

        protected override void Awake()
        {
            base.Awake();
            Initialize(new LogConfig());
        }

        public void Initialize(LogConfig config)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("LogManager已经初始化，跳过重复初始化");
                return;
            }

            try
            {
                _currentLevel = config.DefaultLogLevel;
                _enableTimestamp = config.EnableTimestamp;
                _enableStackTrace = config.EnableStackTrace;

                if (config.EnableConsoleOutput)
                {
                    var consoleOutput = new ConsoleOutput(_enableTimestamp);
                    consoleOutput.SetLogLevel(_currentLevel);
                    _outputs.Add(consoleOutput);
                }

                if (config.EnableFileOutput)
                {
                    var fileOutput = new FileOutput(config.LogFilePath, config.MaxLogFileSize);
                    fileOutput.SetLogLevel(_currentLevel);
                    _outputs.Add(fileOutput);
                }

                if (config.EnableDebugWindow)
                {
                    var debugWindow = DebugWindow.Instance;
                    debugWindow.MaxLines = config.DebugWindowMaxLines;
                    debugWindow.SetLogLevel(_currentLevel);
                    _outputs.Add(debugWindow);
                }

                _isInitialized = true;
                LogInfo("日志系统初始化完成", "LogManager");
            }
            catch (Exception ex)
            {
                Debug.LogError($"LogManager初始化失败: {ex.Message}");
            }
        }

        public void Log(string message, LogLevel level = LogLevel.Info, string tag = null)
        {
            if (level < _currentLevel)
                return;

            string formattedMessage = FormatLogMessage(message, level, tag);

            for (int i = 0; i < _outputs.Count; i++)
            {
                try
                {
                    _outputs[i].Log(formattedMessage, level, tag);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"日志输出失败: {ex.Message}");
                }
            }
        }

        public void LogDebug(string message, string tag = null)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Log(message, LogLevel.Debug, tag);
            #endif
        }

        public void LogInfo(string message, string tag = null)
        {
            Log(message, LogLevel.Info, tag);
        }

        public void LogWarning(string message, string tag = null)
        {
            Log(message, LogLevel.Warning, tag);
        }

        public void LogError(string message, string tag = null)
        {
            Log(message, LogLevel.Error, tag);
        }

        public void LogFatal(string message, string tag = null)
        {
            Log(message, LogLevel.Fatal, tag);
        }

        public void LogException(Exception exception, string tag = null)
        {
            if (exception == null)
                return;

            string message = $"异常: {exception.GetType().Name} - {exception.Message}";
            
            if (_enableStackTrace && exception.StackTrace != null)
            {
                message += $"\n堆栈跟踪:\n{exception.StackTrace}";
            }

            Log(message, LogLevel.Error, tag);
        }

        private string FormatLogMessage(string message, LogLevel level, string tag)
        {
            _messageBuilder.Clear();

            if (_enableTimestamp)
            {
                _messageBuilder.Append('[');
                _messageBuilder.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
                _messageBuilder.Append("] ");
            }

            _messageBuilder.Append(level.ToString().ToUpper());
            _messageBuilder.Append(' ');

            if (!string.IsNullOrEmpty(tag))
            {
                _messageBuilder.Append('[');
                _messageBuilder.Append(tag);
                _messageBuilder.Append("] ");
            }

            _messageBuilder.Append(message);

            return _messageBuilder.ToString();
        }

        public void AddOutput(ILogOutput output)
        {
            if (output == null)
            {
                Debug.LogError("无法添加null输出");
                return;
            }

            if (!_outputs.Contains(output))
            {
                output.SetLogLevel(_currentLevel);
                _outputs.Add(output);
                LogInfo($"添加日志输出: {output.GetType().Name}", "LogManager");
            }
            else
            {
                Debug.LogWarning($"日志输出已存在: {output.GetType().Name}");
            }
        }

        public void RemoveOutput(ILogOutput output)
        {
            if (output == null)
                return;

            if (_outputs.Contains(output))
            {
                _outputs.Remove(output);
                LogInfo($"移除日志输出: {output.GetType().Name}", "LogManager");
            }
        }

        public void SetLogLevel(LogLevel level)
        {
            _currentLevel = level;
            
            for (int i = 0; i < _outputs.Count; i++)
            {
                try
                {
                    _outputs[i].SetLogLevel(level);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"设置日志级别失败: {ex.Message}");
                }
            }

            LogInfo($"日志级别设置为: {level}", "LogManager");
        }

        public void EnableTimestamp(bool enable)
        {
            _enableTimestamp = enable;
        }

        public void EnableStackTrace(bool enable)
        {
            _enableStackTrace = enable;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (var output in _outputs)
            {
                if (output is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"释放日志输出失败: {ex.Message}");
                    }
                }
            }

            _outputs.Clear();
            _messageBuilder.Clear();
            _isInitialized = false;
        }
    }
}
