# 日志调试模块技术文档

## 概述

日志调试模块是基础设施层的核心组件，提供多级别日志记录、内嵌调试窗口、文件输出等功能。该模块采用观察者模式和策略模式，支持多种输出方式，确保日志系统的高内聚、低耦合，同时提供性能优化，确保不影响游戏性能。

## 模块架构设计

### 1. 设计目标

- **多级别日志**：支持Debug、Info、Warning、Error、Fatal五种日志级别
- **多输出方式**：支持控制台输出、文件输出、调试窗口输出等多种方式
- **内嵌调试窗口**：在游戏内实时显示日志信息，便于调试
- **性能优化**：确保日志系统不影响游戏性能，支持异步日志记录
- **易于扩展**：支持自定义日志输出和日志格式

### 2. 架构分层

```
┌─────────────────────────────────────────────────────────────┐
│                    业务逻辑层                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ 角色系统    │  │ 技能系统    │  │ 对战系统    │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    日志管理层                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ LogManager   │  │ LogConfig   │  │ LogLevel    │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    日志输出层                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ Console     │  │ File        │  │ DebugWindow │  │
│  │ Output      │  │ Output      │  │             │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
                    ┌─────────────┐
                    │Unity Engine │
                    │Debug.Log    │
                    │File System  │
                    │GUI System   │
                    └─────────────┘
```

### 3. 核心组件

#### 3.1 日志级别枚举

```csharp
namespace Basement.Logging
{
    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 调试信息 - 最详细的日志，用于开发调试
        /// </summary>
        Debug = 0,

        /// <summary>
        /// 普通信息 - 一般性信息，记录程序运行状态
        /// </summary>
        Info = 1,

        /// <summary>
        /// 警告信息 - 潜在问题，但不影响程序运行
        /// </summary>
        Warning = 2,

        /// <summary>
        /// 错误信息 - 错误情况，但程序可以继续运行
        /// </summary>
        Error = 3,

        /// <summary>
        /// 致命错误 - 严重错误，程序可能无法继续运行
        /// </summary>
        Fatal = 4
    }
}
```

#### 3.2 日志配置类

```csharp
using System;

namespace Basement.Logging
{
    /// <summary>
    /// 日志配置类
    /// </summary>
    [Serializable]
    public class LogConfig
    {
        /// <summary>
        /// 默认日志级别
        /// </summary>
        public LogLevel DefaultLogLevel = LogLevel.Info;

        /// <summary>
        /// 日志文件路径
        /// </summary>
        public string LogFilePath = null;

        /// <summary>
        /// 是否启用控制台输出
        /// </summary>
        public bool EnableConsoleOutput = true;

        /// <summary>
        /// 是否启用文件输出
        /// </summary>
        public bool EnableFileOutput = true;

        /// <summary>
        /// 是否启用调试窗口
        /// </summary>
        public bool EnableDebugWindow = true;

        /// <summary>
        /// 调试窗口最大行数
        /// </summary>
        public int DebugWindowMaxLines = 1000;

        /// <summary>
        /// 是否启用时间戳
        /// </summary>
        public bool EnableTimestamp = true;

        /// <summary>
        /// 是否启用堆栈跟踪
        /// </summary>
        public bool EnableStackTrace = false;

        /// <summary>
        /// 最大日志文件大小（MB）
        /// </summary>
        public int MaxLogFileSize = 10;

        /// <summary>
        /// 日志文件保留数量
        /// </summary>
        public int MaxLogFileCount = 5;

        /// <summary>
        /// 是否异步写入日志
        /// </summary>
        public bool EnableAsyncLogging = true;

        /// <summary>
        /// 异步日志队列大小
        /// </summary>
        public int AsyncLogQueueSize = 1000;
    }
}
```

#### 3.3 日志输出接口

```csharp
namespace Basement.Logging
{
    /// <summary>
    /// 日志输出接口
    /// 定义日志输出的标准操作
    /// </summary>
    public interface ILogOutput
    {
        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="level">日志级别</param>
        /// <param name="tag">日志标签</param>
        void Log(string message, LogLevel level, string tag = null);

        /// <summary>
        /// 设置日志级别
        /// </summary>
        /// <param name="level">日志级别</param>
        void SetLogLevel(LogLevel level);

        /// <summary>
        /// 获取当前日志级别
        /// </summary>
        /// <returns>当前日志级别</returns>
        LogLevel GetLogLevel();
    }
}
```

#### 3.4 控制台输出实现

```csharp
using UnityEngine;

namespace Basement.Logging
{
    /// <summary>
    /// 控制台输出实现
    /// 将日志输出到Unity控制台
    /// </summary>
    public class ConsoleOutput : ILogOutput
    {
        private LogLevel _currentLevel = LogLevel.Debug;

        public void Log(string message, LogLevel level, string tag = null)
        {
            if (level < _currentLevel) return;

            string formattedMessage = FormatMessage(message, level, tag);

            switch (level)
            {
                case LogLevel.Debug:
                    Debug.Log(formattedMessage);
                    break;
                case LogLevel.Info:
                    Debug.Log(formattedMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                    Debug.LogError(formattedMessage);
                    break;
                case LogLevel.Fatal:
                    Debug.LogError(formattedMessage);
                    break;
            }
        }

        public void SetLogLevel(LogLevel level)
        {
            _currentLevel = level;
        }

        public LogLevel GetLogLevel()
        {
            return _currentLevel;
        }

        private string FormatMessage(string message, LogLevel level, string tag)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            string levelStr = level.ToString().ToUpper();
            string tagStr = string.IsNullOrEmpty(tag) ? "" : $"[{tag}] ";

            return $"[{timestamp}] [{levelStr}] {tagStr}{message}";
        }
    }
}
```

#### 3.5 文件输出实现

```csharp
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Basement.Logging
{
    /// <summary>
    /// 文件输出实现
    /// 将日志输出到文件
    /// </summary>
    public class FileOutput : ILogOutput
    {
        private readonly string _logFilePath;
        private readonly int _maxFileSize;
        private readonly int _maxFileCount;
        private readonly object _lock = new object();
        private LogLevel _currentLevel = LogLevel.Debug;
        private StreamWriter _writer;
        private long _currentFileSize;

        public FileOutput(string filePath, int maxFileSize = 10, int maxFileCount = 5)
        {
            _logFilePath = filePath;
            _maxFileSize = maxFileSize * 1024 * 1024; // 转换为字节
            _maxFileCount = maxFileCount;

            InitializeFile();
        }

        private void InitializeFile()
        {
            lock (_lock)
            {
                // 确保日志目录存在
                string directory = Path.GetDirectoryName(_logFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 检查文件大小，如果超过限制则轮转
                if (File.Exists(_logFilePath))
                {
                    _currentFileSize = new FileInfo(_logFilePath).Length;
                    if (_currentFileSize >= _maxFileSize)
                    {
                        RotateLogFile();
                    }
                }

                // 打开日志文件
                _writer = new StreamWriter(_logFilePath, true, Encoding.UTF8);
                _writer.AutoFlush = true;

                // 写入启动信息
                _writer.WriteLine($"===== Log Started at {DateTime.Now} =====");
            }
        }

        public void Log(string message, LogLevel level, string tag = null)
        {
            if (level < _currentLevel) return;

            string formattedMessage = FormatMessage(message, level, tag);

            lock (_lock)
            {
                try
                {
                    _writer.WriteLine(formattedMessage);
                    _currentFileSize += Encoding.UTF8.GetByteCount(formattedMessage) + Environment.NewLine.Length;

                    // 检查文件大小
                    if (_currentFileSize >= _maxFileSize)
                    {
                        RotateLogFile();
                    }
                }
                catch (Exception ex)
                {
                    // 避免日志系统崩溃
                    UnityEngine.Debug.LogError($"日志写入失败: {ex.Message}");
                }
            }
        }

        public void SetLogLevel(LogLevel level)
        {
            _currentLevel = level;
        }

        public LogLevel GetLogLevel()
        {
            return _currentLevel;
        }

        private string FormatMessage(string message, LogLevel level, string tag)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string levelStr = level.ToString().ToUpper();
            string tagStr = string.IsNullOrEmpty(tag) ? "" : $"[{tag}] ";

            return $"[{timestamp}] [{levelStr}] {tagStr}{message}";
        }

        private void RotateLogFile()
        {
            // 关闭当前文件
            _writer?.Close();
            _writer = null;

            // 轮转日志文件
            for (int i = _maxFileCount - 1; i > 0; i--)
            {
                string oldFile = GetLogFileName(i);
                string newFile = GetLogFileName(i + 1);

                if (File.Exists(oldFile))
                {
                    if (File.Exists(newFile))
                    {
                        File.Delete(newFile);
                    }
                    File.Move(oldFile, newFile);
                }
            }

            // 移动当前文件
            if (File.Exists(_logFilePath))
            {
                string rotatedFile = GetLogFileName(1);
                if (File.Exists(rotatedFile))
                {
                    File.Delete(rotatedFile);
                }
                File.Move(_logFilePath, rotatedFile);
            }

            // 创建新文件
            _writer = new StreamWriter(_logFilePath, true, Encoding.UTF8);
            _writer.AutoFlush = true;
            _currentFileSize = 0;

            _writer.WriteLine($"===== Log Rotated at {DateTime.Now} =====");
        }

        private string GetLogFileName(int index)
        {
            string directory = Path.GetDirectoryName(_logFilePath);
            string fileName = Path.GetFileNameWithoutExtension(_logFilePath);
            string extension = Path.GetExtension(_logFilePath);

            return Path.Combine(directory, $"{fileName}.{index}{extension}");
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _writer?.Close();
                _writer = null;
            }
        }
    }
}
```

#### 3.6 调试窗口实现

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Basement.Logging
{
    /// <summary>
    /// 调试窗口实现
    /// 在游戏内显示日志信息
    /// </summary>
    public class DebugWindow : MonoBehaviour, ILogOutput
    {
        [SerializeField] private bool showWindow = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F12;
        [SerializeField] private int maxLines = 1000;
        [SerializeField] private float windowWidth = 600f;
        [SerializeField] private float windowHeight = 400f;
        [SerializeField] private int fontSize = 12;

        private readonly List<LogEntry> _logEntries = new List<LogEntry>();
        private LogLevel _currentLevel = LogLevel.Debug;
        private Vector2 _scrollPosition;
        private bool _filterDebug = true;
        private bool _filterInfo = true;
        private bool _filterWarning = true;
        private bool _filterError = true;
        private bool _filterFatal = true;
        private string _searchText = "";
        private LogLevel _minLogLevel = LogLevel.Debug;

        private class LogEntry
        {
            public string Message { get; set; }
            public LogLevel Level { get; set; }
            public string Tag { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public void Log(string message, LogLevel level, string tag = null)
        {
            if (level < _currentLevel) return;

            var entry = new LogEntry
            {
                Message = message,
                Level = level,
                Tag = tag,
                Timestamp = DateTime.Now
            };

            lock (_logEntries)
            {
                _logEntries.Add(entry);

                // 限制日志条数
                while (_logEntries.Count > maxLines)
                {
                    _logEntries.RemoveAt(0);
                }
            }
        }

        public void SetLogLevel(LogLevel level)
        {
            _currentLevel = level;
        }

        public LogLevel GetLogLevel()
        {
            return _currentLevel;
        }

        private void Update()
        {
            // 切换窗口显示
            if (Input.GetKeyDown(toggleKey))
            {
                showWindow = !showWindow;
            }
        }

        private void OnGUI()
        {
            if (!showWindow) return;

            // 创建窗口
            Rect windowRect = new Rect(10, 10, windowWidth, windowHeight);
            windowRect = GUILayout.Window(0, windowRect, DrawWindow, "调试窗口");

            // 确保窗口在屏幕内
            windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - windowWidth);
            windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - windowHeight);
        }

        private void DrawWindow(int windowID)
        {
            // 过滤器
            DrawFilters();

            // 搜索框
            DrawSearchBox();

            // 日志列表
            DrawLogList();

            // 清除按钮
            if (GUILayout.Button("清除日志"))
            {
                lock (_logEntries)
                {
                    _logEntries.Clear();
                }
            }

            GUI.DragWindow();
        }

        private void DrawFilters()
        {
            GUILayout.BeginHorizontal();
            _filterDebug = GUILayout.Toggle(_filterDebug, "Debug");
            _filterInfo = GUILayout.Toggle(_filterInfo, "Info");
            _filterWarning = GUILayout.Toggle(_filterWarning, "Warning");
            _filterError = GUILayout.Toggle(_filterError, "Error");
            _filterFatal = GUILayout.Toggle(_filterFatal, "Fatal");
            GUILayout.EndHorizontal();
        }

        private void DrawSearchBox()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", GUILayout.Width(40));
            _searchText = GUILayout.TextField(_searchText);
            GUILayout.EndHorizontal();
        }

        private void DrawLogList()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            lock (_logEntries)
            {
                foreach (var entry in _logEntries)
                {
                    if (!ShouldShowEntry(entry)) continue;

                    DrawLogEntry(entry);
                }
            }

            GUILayout.EndScrollView();
        }

        private bool ShouldShowEntry(LogEntry entry)
        {
            // 检查级别过滤
            if (entry.Level == LogLevel.Debug && !_filterDebug) return false;
            if (entry.Level == LogLevel.Info && !_filterInfo) return false;
            if (entry.Level == LogLevel.Warning && !_filterWarning) return false;
            if (entry.Level == LogLevel.Error && !_filterError) return false;
            if (entry.Level == LogLevel.Fatal && !_filterFatal) return false;

            // 检查级别限制
            if (entry.Level < _minLogLevel) return false;

            // 检查搜索文本
            if (!string.IsNullOrEmpty(_searchText))
            {
                string searchText = _searchText.ToLower();
                string message = entry.Message.ToLower();
                string tag = (entry.Tag ?? "").ToLower();

                if (!message.Contains(searchText) && !tag.Contains(searchText))
                {
                    return false;
                }
            }

            return true;
        }

        private void DrawLogEntry(LogEntry entry)
        {
            string timestamp = entry.Timestamp.ToString("HH:mm:ss.fff");
            string levelStr = entry.Level.ToString().ToUpper();
            string tagStr = string.IsNullOrEmpty(entry.Tag) ? "" : $"[{entry.Tag}] ";
            string message = $"[{timestamp}] [{levelStr}] {tagStr}{entry.Message}";

            // 根据日志级别设置颜色
            GUI.color = GetLogColor(entry.Level);
            GUILayout.Label(message, new GUIStyle(GUI.skin.label) { fontSize = fontSize });
            GUI.color = Color.white;
        }

        private Color GetLogColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return Color.gray;
                case LogLevel.Info:
                    return Color.white;
                case LogLevel.Warning:
                    return Color.yellow;
                case LogLevel.Error:
                    return Color.red;
                case LogLevel.Fatal:
                    return new Color(1f, 0f, 0f, 1f); // 深红色
                default:
                    return Color.white;
            }
        }
    }
}
```

#### 3.7 日志管理器

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using Basement.Utils;

namespace Basement.Logging
{
    /// <summary>
    /// 日志管理器
    /// 负责管理所有日志输出和日志级别
    /// </summary>
    public class LogManager : Singleton<LogManager>
    {
        private readonly List<ILogOutput> _outputs = new List<ILogOutput>();
        private LogLevel _currentLevel = LogLevel.Debug;
        private readonly object _lock = new object();
        private bool _isInitialized = false;
        private bool _enableAsyncLogging = true;
        private readonly Queue<LogMessage> _asyncLogQueue = new Queue<LogMessage>();
        private Thread _asyncLogThread;
        private bool _isRunning = false;
        private readonly int _maxQueueSize = 1000;

        private class LogMessage
        {
            public string Message { get; set; }
            public LogLevel Level { get; set; }
            public string Tag { get; set; }
        }

        public LogLevel CurrentLevel => _currentLevel;

        public void Initialize(LogConfig config)
        {
            if (_isInitialized)
            {
                UnityEngine.Debug.LogWarning("LogManager已经初始化");
                return;
            }

            lock (_lock)
            {
                // 设置日志级别
                _currentLevel = config.DefaultLogLevel;
                _enableAsyncLogging = config.EnableAsyncLogging;

                // 清除现有输出
                _outputs.Clear();

                // 添加控制台输出
                if (config.EnableConsoleOutput)
                {
                    AddOutput(new ConsoleOutput());
                }

                // 添加文件输出
                if (config.EnableFileOutput && !string.IsNullOrEmpty(config.LogFilePath))
                {
                    AddOutput(new FileOutput(config.LogFilePath, config.MaxLogFileSize, config.MaxLogFileCount));
                }

                // 添加调试窗口
                if (config.EnableDebugWindow)
                {
                    var debugWindowObj = new UnityEngine.GameObject("[DebugWindow]");
                    var debugWindow = debugWindowObj.AddComponent<DebugWindow>();
                    AddOutput(debugWindow);
                    UnityEngine.Object.DontDestroyOnLoad(debugWindowObj);
                }

                // 启动异步日志线程
                if (_enableAsyncLogging)
                {
                    StartAsyncLogging();
                }

                _isInitialized = true;
                UnityEngine.Debug.Log("LogManager初始化完成");
            }
        }

        public void AddOutput(ILogOutput output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));

            lock (_lock)
            {
                output.SetLogLevel(_currentLevel);
                _outputs.Add(output);
            }
        }

        public void RemoveOutput(ILogOutput output)
        {
            if (output == null) return;

            lock (_lock)
            {
                _outputs.Remove(output);
            }
        }

        public void SetLogLevel(LogLevel level)
        {
            lock (_lock)
            {
                _currentLevel = level;

                foreach (var output in _outputs)
                {
                    output.SetLogLevel(level);
                }
            }
        }

        public void LogDebug(string message, string tag = null)
        {
            Log(message, LogLevel.Debug, tag);
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
            string message = $"Exception: {exception.Message}\nStack Trace:\n{exception.StackTrace}";
            Log(message, LogLevel.Error, tag);
        }

        public void Log(string message, LogLevel level, string tag = null)
        {
            if (level < _currentLevel) return;

            if (_enableAsyncLogging)
            {
                EnqueueAsyncLog(message, level, tag);
            }
            else
            {
                LogSync(message, level, tag);
            }
        }

        private void LogSync(string message, LogLevel level, string tag)
        {
            lock (_lock)
            {
                foreach (var output in _outputs)
                {
                    try
                    {
                        output.Log(message, level, tag);
                    }
                    catch (Exception ex)
                    {
                        // 避免日志系统崩溃
                        UnityEngine.Debug.LogError($"日志输出失败: {ex.Message}");
                    }
                }
            }
        }

        private void EnqueueAsyncLog(string message, LogLevel level, string tag)
        {
            lock (_asyncLogQueue)
            {
                if (_asyncLogQueue.Count >= _maxQueueSize)
                {
                    // 队列已满，丢弃最旧的日志
                    _asyncLogQueue.Dequeue();
                }

                _asyncLogQueue.Enqueue(new LogMessage
                {
                    Message = message,
                    Level = level,
                    Tag = tag
                });
            }
        }

        private void StartAsyncLogging()
        {
            _isRunning = true;
            _asyncLogThread = new Thread(AsyncLoggingLoop)
            {
                IsBackground = true,
                Name = "AsyncLoggingThread"
            };
            _asyncLogThread.Start();
        }

        private void AsyncLoggingLoop()
        {
            while (_isRunning)
            {
                LogMessage message = null;

                lock (_asyncLogQueue)
                {
                    if (_asyncLogQueue.Count > 0)
                    {
                        message = _asyncLogQueue.Dequeue();
                    }
                }

                if (message != null)
                {
                    LogSync(message.Message, message.Level, message.Tag);
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 停止异步日志线程
            _isRunning = false;

            if (_asyncLogThread != null && _asyncLogThread.IsAlive)
            {
                _asyncLogThread.Join(1000);
            }

            // 清理输出
            lock (_lock)
            {
                foreach (var output in _outputs)
                {
                    if (output is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _outputs.Clear();
            }
        }
    }
}
```

## 使用说明

### 1. 基础使用

```csharp
using UnityEngine;
using Basement.Logging;

public class GameInitializer : MonoBehaviour
{
    private void Start()
    {
        // 初始化日志系统
        var config = new LogConfig
        {
            DefaultLogLevel = LogLevel.Debug,
            LogFilePath = "Logs/Game.log",
            EnableConsoleOutput = true,
            EnableFileOutput = true,
            EnableDebugWindow = true
        };

        LogManager.Instance.Initialize(config);

        // 记录日志
        LogManager.Instance.LogInfo("游戏初始化完成", "Game");

        // 记录不同级别的日志
        LogManager.Instance.LogDebug("这是调试信息", "Test");
        LogManager.Instance.LogInfo("这是普通信息", "Test");
        LogManager.Instance.LogWarning("这是警告信息", "Test");
        LogManager.Instance.LogError("这是错误信息", "Test");
        LogManager.Instance.LogFatal("这是致命错误", "Test");
    }
}
```

### 2. 异常处理

```csharp
using UnityEngine;
using Basement.Logging;

public class DataProcessor : MonoBehaviour
{
    public void ProcessData(string data)
    {
        try
        {
            // 处理数据
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentException("数据不能为空");
            }

            LogManager.Instance.LogInfo($"数据处理成功: {data}", "DataProcessor");
        }
        catch (Exception ex)
        {
            LogManager.Instance.LogException(ex, "DataProcessor");
        }
    }
}
```

### 3. 日志级别控制

```csharp
using UnityEngine;
using Basement.Logging;

public class LogLevelController : MonoBehaviour
{
    private void Update()
    {
        // 按F1切换到Debug级别
        if (Input.GetKeyDown(KeyCode.F1))
        {
            LogManager.Instance.SetLogLevel(LogLevel.Debug);
            Debug.Log("日志级别设置为Debug");
        }

        // 按F2切换到Info级别
        if (Input.GetKeyDown(KeyCode.F2))
        {
            LogManager.Instance.SetLogLevel(LogLevel.Info);
            Debug.Log("日志级别设置为Info");
        }

        // 按F3切换到Warning级别
        if (Input.GetKeyDown(KeyCode.F3))
        {
            LogManager.Instance.SetLogLevel(LogLevel.Warning);
            Debug.Log("日志级别设置为Warning");
        }

        // 按F4切换到Error级别
        if (Input.GetKeyDown(KeyCode.F4))
        {
            LogManager.Instance.SetLogLevel(LogLevel.Error);
            Debug.Log("日志级别设置为Error");
        }
    }
}
```

### 4. 自定义日志输出

```csharp
using UnityEngine;
using Basement.Logging;

public class CustomLogOutput : ILogOutput
{
    private LogLevel _currentLevel = LogLevel.Debug;

    public void Log(string message, LogLevel level, string tag = null)
    {
        if (level < _currentLevel) return;

        // 自定义日志输出逻辑
        // 例如：发送到远程服务器、写入数据库等
        Debug.Log($"自定义输出: [{level}] {tag} {message}");
    }

    public void SetLogLevel(LogLevel level)
    {
        _currentLevel = level;
    }

    public LogLevel GetLogLevel()
    {
        return _currentLevel;
    }
}

public class CustomOutputExample : MonoBehaviour
{
    private void Start()
    {
        // 添加自定义输出
        var customOutput = new CustomLogOutput();
        LogManager.Instance.AddOutput(customOutput);
    }
}
```

## 性能优化策略

### 1. 异步日志记录

```csharp
// 在LogConfig中启用异步日志
var config = new LogConfig
{
    EnableAsyncLogging = true,
    AsyncLogQueueSize = 1000
};

LogManager.Instance.Initialize(config);
```

### 2. 日志级别过滤

```csharp
// 在生产环境中设置较高的日志级别
#if !UNITY_EDITOR
var config = new LogConfig
{
    DefaultLogLevel = LogLevel.Warning
};
#else
var config = new LogConfig
{
    DefaultLogLevel = LogLevel.Debug
};
#endif
```

### 3. 条件编译

```csharp
// 使用条件编译完全移除Debug日志
public void LogDebug(string message, string tag = null)
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    Log(message, LogLevel.Debug, tag);
#endif
}
```

### 4. 日志缓冲

```csharp
public class BufferedLogOutput : ILogOutput
{
    private readonly List<string> _buffer = new List<string>();
    private readonly ILogOutput _baseOutput;
    private readonly int _bufferSize;
    private readonly object _lock = new object();

    public BufferedLogOutput(ILogOutput baseOutput, int bufferSize = 100)
    {
        _baseOutput = baseOutput;
        _bufferSize = bufferSize;
    }

    public void Log(string message, LogLevel level, string tag = null)
    {
        lock (_lock)
        {
            _buffer.Add(message);

            if (_buffer.Count >= _bufferSize)
            {
                Flush();
            }
        }
    }

    public void Flush()
    {
        lock (_lock)
        {
            foreach (var message in _buffer)
            {
                _baseOutput.Log(message, LogLevel.Info);
            }
            _buffer.Clear();
        }
    }

    public void SetLogLevel(LogLevel level)
    {
        _baseOutput.SetLogLevel(level);
    }

    public LogLevel GetLogLevel()
    {
        return _baseOutput.GetLogLevel();
    }
}
```

### 5. 日志采样

```csharp
public class SamplingLogOutput : ILogOutput
{
    private readonly ILogOutput _baseOutput;
    private readonly float _samplingRate;
    private float _accumulatedTime = 0f;

    public SamplingLogOutput(ILogOutput baseOutput, float samplingRate = 0.1f)
    {
        _baseOutput = baseOutput;
        _samplingRate = samplingRate;
    }

    public void Log(string message, LogLevel level, string tag = null)
    {
        _accumulatedTime += Time.deltaTime;

        if (_accumulatedTime >= _samplingRate)
        {
            _baseOutput.Log(message, level, tag);
            _accumulatedTime = 0f;
        }
    }

    public void SetLogLevel(LogLevel level)
    {
        _baseOutput.SetLogLevel(level);
    }

    public LogLevel GetLogLevel()
    {
        return _baseOutput.GetLogLevel();
    }
}
```

## 与Unity引擎的结合点

### 1. 编辑器集成

```csharp
#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public class LogManagerEditor
{
    static LogManagerEditor()
    {
        // 在编辑器启动时初始化日志系统
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // 进入播放模式时初始化日志系统
            var config = new LogConfig
            {
                DefaultLogLevel = LogLevel.Debug,
                EnableConsoleOutput = true,
                EnableDebugWindow = true
            };

            LogManager.Instance.Initialize(config);
        }
    }
}
#endif
```

### 2. Unity事件集成

```csharp
using UnityEngine;
using Basement.Logging;

public class UnityEventLogger : MonoBehaviour
{
    private void OnEnable()
    {
        // 订阅Unity事件
        Application.logMessageReceived += HandleUnityLog;
    }

    private void OnDisable()
    {
        // 取消订阅Unity事件
        Application.logMessageReceived -= HandleUnityLog;
    }

    private void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        // 将Unity日志转换为系统日志
        LogLevel level = ConvertUnityLogType(type);
        LogManager.Instance.Log(logString, level, "Unity");
    }

    private LogLevel ConvertUnityLogType(LogType type)
    {
        switch (type)
        {
            case LogType.Log:
                return LogLevel.Info;
            case LogType.Warning:
                return LogLevel.Warning;
            case LogType.Error:
                return LogLevel.Error;
            case LogType.Exception:
                return LogLevel.Error;
            case LogType.Assert:
                return LogLevel.Fatal;
            default:
                return LogLevel.Info;
        }
    }
}
```

### 3. 性能监控

```csharp
using UnityEngine;
using Basement.Logging;

public class LogPerformanceMonitor : MonoBehaviour
{
    [SerializeField] private float updateInterval = 1f;
    private float _accumulatedTime = 0f;
    private int _logCount = 0;

    private void Update()
    {
        _accumulatedTime += Time.deltaTime;

        if (_accumulatedTime >= updateInterval)
        {
            float logsPerSecond = _logCount / _accumulatedTime;
            LogManager.Instance.LogInfo($"日志性能: {logsPerSecond:F1} logs/sec", "Performance");

            _accumulatedTime = 0f;
            _logCount = 0;
        }
    }

    public void OnLogWritten()
    {
        _logCount++;
    }
}
```

## 总结

日志调试模块通过多级别日志记录、多输出方式和性能优化，提供了完整的日志调试功能，具有以下优势：

1. **多级别支持**：支持Debug、Info、Warning、Error、Fatal五种日志级别
2. **多输出方式**：支持控制台、文件、调试窗口等多种输出方式
3. **性能优化**：通过异步日志记录、级别过滤等技术确保性能
4. **易于扩展**：支持自定义日志输出和日志格式
5. **内嵌调试**：提供游戏内调试窗口，便于实时查看日志

通过使用日志调试模块，项目可以实现高效的日志管理和调试，提高开发效率和问题排查能力。
