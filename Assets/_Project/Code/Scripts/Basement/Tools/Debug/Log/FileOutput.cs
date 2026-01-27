using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Basement.Logging
{
    public class FileOutput : ILogOutput, IDisposable
    {
        private LogLevel _logLevel = LogLevel.Info;
        private string _filePath;
        private StreamWriter _writer;
        private readonly object _lock = new object();
        private long _currentFileSize;
        private int _maxFileSizeMB = 10;
        private bool _disposed = false;

        public FileOutput(string filePath = null, int maxFileSizeMB = 10)
        {
            _maxFileSizeMB = maxFileSizeMB;
            _filePath = filePath ?? GetDefaultLogPath();
            InitializeFile();
        }

        private string GetDefaultLogPath()
        {
            string logDir = Path.Combine(Application.persistentDataPath, "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            return Path.Combine(logDir, $"game_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        }

        private void InitializeFile()
        {
            try
            {
                _writer = new StreamWriter(_filePath, false)
                {
                    AutoFlush = false
                };
                _currentFileSize = 0;
                Log("日志文件初始化完成", LogLevel.Info, "FileOutput");
                Flush();
            }
            catch (Exception e)
            {
                Debug.LogError($"创建日志文件失败: {e.Message}");
            }
        }

        public void Log(string message, LogLevel level, string tag = null)
        {
            if (level < _logLevel || _writer == null || _disposed)
                return;

            lock (_lock)
            {
                try
                {
                    string logLine = message + Environment.NewLine;
                    _writer.Write(logLine);
                    _currentFileSize += System.Text.Encoding.UTF8.GetByteCount(logLine);

                    if (_currentFileSize > _maxFileSizeMB * 1024 * 1024)
                    {
                        RotateLogFile();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"写入日志文件失败: {e.Message}");
                }
            }
        }

        public void SetLogLevel(LogLevel level)
        {
            _logLevel = level;
        }

        public void Flush()
        {
            lock (_lock)
            {
                try
                {
                    _writer?.Flush();
                }
                catch (Exception e)
                {
                    Debug.LogError($"刷新日志文件失败: {e.Message}");
                }
            }
        }

        private void RotateLogFile()
        {
            try
            {
                _writer?.Close();
                _writer?.Dispose();

                string archivePath = _filePath.Replace(".log", "_old.log");
                if (File.Exists(archivePath))
                {
                    File.Delete(archivePath);
                }
                File.Move(_filePath, archivePath);

                InitializeFile();
            }
            catch (Exception e)
            {
                Debug.LogError($"轮转日志文件失败: {e.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            lock (_lock)
            {
                try
                {
                    _writer?.Flush();
                    _writer?.Close();
                    _writer?.Dispose();
                    _writer = null;
                }
                catch (Exception e)
                {
                    Debug.LogError($"关闭日志文件失败: {e.Message}");
                }
            }
        }

        ~FileOutput()
        {
            Dispose();
        }
    }
}
