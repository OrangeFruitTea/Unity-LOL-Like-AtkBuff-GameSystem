# Basement/Tools模块独立性评估报告

## 1. 概述

本报告对Basement/Tools模块进行全面独立性评估，分析其与项目其他模块的依赖关系，评估接口设计的合理性，并提供降低耦合度的解决方案。

---

## 2. 当前依赖关系图谱

### 2.1 Tools模块结构

```
Basement/Tools/
├── Debug/
│   ├── Log/                          # 日志系统（核心功能）
│   │   ├── LogLevel.cs               # 日志级别枚举
│   │   ├── ILogOutput.cs             # 日志输出接口
│   │   ├── LogConfig.cs              # 日志配置类
│   │   ├── ConsoleOutput.cs          # 控制台输出实现
│   │   ├── FileOutput.cs             # 文件输出实现
│   │   ├── DebugWindow.cs            # 调试窗口实现
│   │   └── LogManager.cs              # 日志管理器（核心）
│   ├── Tests/                        # 测试脚本
│   │   └── LogManagerTest.cs
│   ├── BuffDebugger.cs               # Buff调试工具（业务相关）
│   └── TestPlayerSpawner.cs          # 测试玩家生成器（业务相关）
```

### 2.2 外部依赖分析

#### 2.2.1 基础设施依赖

| 依赖项 | 使用位置 | 依赖类型 | 迁移影响 |
|--------|----------|----------|----------|
| **Basement.Utils.Singleton** | LogManager.cs, DebugWindow.cs | 基类继承 | **高** - 核心依赖 |
| **UnityEditor** | DebugWindow.cs | 编辑器API | **中** - 运行时限制 |
| **UnityEngine** | 所有文件 | Unity原生 | **低** - 标准依赖 |

#### 2.2.2 业务模块依赖

| 依赖项 | 使用位置 | 依赖类型 | 迁移影响 |
|--------|----------|----------|----------|
| **Core.ECS** | TestPlayerSpawner.cs | 业务逻辑 | **高** - 强耦合 |
| **Core.Entity** | TestPlayerSpawner.cs | 业务逻辑 | **高** - 强耦合 |
| **BuffSystem** | BuffDebugger.cs | 业务逻辑 | **高** - 强耦合 |

### 2.3 依赖关系图

```
┌─────────────────────────────────────────────────────────────┐
│                    Basement/Tools 模块                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────┐         ┌──────────────────┐          │
│  │  Log System     │         │  Test Scripts    │          │
│  │  (核心功能)      │         │  (测试工具)       │          │
│  │                  │         │                  │          │
│  │  - LogLevel      │         │  - BuffDebugger  │          │
│  │  - ILogOutput    │         │  - TestPlayer    │          │
│  │  - LogManager    │         │    Spawner       │          │
│  │  - ConsoleOutput │         │                  │          │
│  │  - FileOutput    │         │                  │          │
│  │  - DebugWindow   │         │                  │          │
│  └────────┬─────────┘         └────────┬─────────┘          │
│           │                            │                     │
│           │ 依赖                        │ 依赖                │
│           ▼                            ▼                     │
│  ┌──────────────────┐         ┌──────────────────┐          │
│  │ Basement.Utils   │         │  Core.ECS        │          │
│  │ .Singleton       │         │  Core.Entity     │          │
│  │                  │         │  BuffSystem      │          │
│  └──────────────────┘         └──────────────────┘          │
│                                                              │
└─────────────────────────────────────────────────────────────┘

外部使用方：
┌──────────────────┐         ┌──────────────────┐
│ FileJsonStorage  │         │ BuffDataLoader   │
│ (Basement/Json)  │         │ (Gameplay)       │
└──────────────────┘         └──────────────────┘
```

---

## 3. 接口设计评估

### 3.1 公共接口分析

#### 3.1.1 LogManager公共接口

```csharp
// 核心日志接口
public void Log(string message, LogLevel level = LogLevel.Info, string tag = null)
public void LogDebug(string message, string tag = null)
public void LogInfo(string message, string tag = null)
public void LogWarning(string message, string tag = null)
public void LogError(string message, string tag = null)
public void LogFatal(string message, string tag = null)
public void LogException(Exception exception, string tag = null)

// 配置接口
public void Initialize(LogConfig config)
public void SetLogLevel(LogLevel level)
public void EnableTimestamp(bool enable)
public void EnableStackTrace(bool enable)

// 扩展接口
public void AddOutput(ILogOutput output)
public void RemoveOutput(ILogOutput output)
public LogLevel CurrentLevel { get; }
```

#### 3.1.2 ILogOutput接口

```csharp
public interface ILogOutput
{
    void Log(string message, LogLevel level, string tag = null);
    void SetLogLevel(LogLevel level);
}
```

### 3.2 接口设计评估结果

| 评估维度 | 评分 | 说明 |
|----------|------|------|
| **自包含性** | ⭐⭐⭐⭐☆ | 核心日志接口设计良好，但依赖Singleton基类 |
| **一致性** | ⭐⭐⭐⭐⭐ | 日志方法命名统一，参数结构一致 |
| **易用性** | ⭐⭐⭐⭐⭐ | 提供便捷方法，支持标签系统 |
| **扩展性** | ⭐⭐⭐⭐⭐ | 通过ILogOutput接口支持自定义输出 |
| **可测试性** | ⭐⭐⭐⭐☆ | 提供测试脚本，但依赖Unity环境 |

### 3.3 接口设计优点

1. **清晰的分层设计**：
   - 接口层（ILogOutput）
   - 实现层（ConsoleOutput, FileOutput, DebugWindow）
   - 管理层（LogManager）

2. **灵活的配置系统**：
   - 支持多种输出目标
   - 可配置日志级别
   - 支持时间戳和堆栈跟踪

3. **良好的扩展性**：
   - 通过接口实现自定义输出
   - 支持动态添加/移除输出目标

### 3.4 接口设计问题

1. **Singleton依赖**：
   - LogManager继承自Singleton<T>
   - 迁移时需要携带Singleton基类

2. **DebugWindow编辑器依赖**：
   - 使用EditorStyles.toolbar
   - 限制在运行时使用

3. **初始化耦合**：
   - Awake方法中自动初始化
   - 缺少手动控制初始化的选项

---

## 4. 迁移难度评级

### 4.1 整体迁移难度

| 模块 | 难度评级 | 所需工作量 | 风险等级 |
|------|----------|------------|----------|
| **Log系统核心** | ⭐⭐☆☆☆ | 2-4小时 | 低 |
| **DebugWindow** | ⭐⭐⭐☆☆ | 4-6小时 | 中 |
| **测试脚本** | ⭐⭐⭐⭐☆ | 6-8小时 | 高 |
| **业务工具** | ⭐⭐⭐⭐⭐ | 8-12小时 | 高 |

### 4.2 迁移挑战分析

#### 4.2.1 核心日志系统（难度：⭐⭐☆☆☆）

**迁移步骤**：
1. 复制Log文件夹到目标项目
2. 复制或重新实现Singleton基类
3. 处理DebugWindow的编辑器依赖
4. 测试基本功能

**潜在问题**：
- Singleton基类的实现差异
- DebugWindow在运行时的兼容性

#### 4.2.2 DebugWindow（难度：⭐⭐⭐☆☆）

**迁移步骤**：
1. 移除EditorStyles.toolbar依赖
2. 使用GUI.skin替代编辑器样式
3. 测试运行时显示功能

**潜在问题**：
- 样式可能不如编辑器美观
- 需要重新设计UI布局

#### 4.2.3 测试脚本（难度：⭐⭐⭐⭐☆）

**迁移步骤**：
1. 修改BuffDebugger.cs，移除业务依赖
2. 修改TestPlayerSpawner.cs，移除ECS依赖
3. 创建通用测试框架

**潜在问题**：
- 测试脚本与业务逻辑强耦合
- 需要重新设计测试用例

#### 4.2.4 业务工具（难度：⭐⭐⭐⭐⭐）

**迁移步骤**：
1. 完全重写BuffDebugger.cs
2. 完全重写TestPlayerSpawner.cs
3. 设计通用的调试工具框架

**潜在问题**：
- 业务逻辑无法复用
- 需要针对新项目重新设计

---

## 5. 桥接脚本详细设计

### 5.1 设计目标

1. **解耦Tools模块与项目业务逻辑**
2. **提供统一的接口适配层**
3. **支持依赖注入机制**
4. **实现配置隔离**

### 5.2 架构设计

```
┌─────────────────────────────────────────────────────────────┐
│                    桥接层架构设计                            │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              应用层（业务代码）                        │   │
│  │  - FileJsonStorage                                    │   │
│  │  - BuffDataLoader                                     │   │
│  │  - 其他业务模块                                        │   │
│  └────────────────────┬─────────────────────────────────┘   │
│                       │                                       │
│                       │ 调用                                  │
│                       ▼                                       │
│  ┌──────────────────────────────────────────────────────┐   │
│  │           接口适配层（Bridge）                          │   │
│  │  - ILogAdapter                                        │   │
│  │  - LogBridge                                          │   │
│  │  - SingletonAdapter                                   │   │
│  └────────────────────┬─────────────────────────────────┘   │
│                       │                                       │
│                       │ 适配                                  │
│                       ▼                                       │
│  ┌──────────────────────────────────────────────────────┐   │
│  │         Tools模块（可迁移组件）                        │   │
│  │  - LogLevel                                           │   │
│  │  - ILogOutput                                        │   │
│  │  - ConsoleOutput                                     │   │
│  │  - FileOutput                                        │   │
│  │  - DebugWindow                                       │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │         基础设施层（项目特定）                          │   │
│  │  - Singleton基类                                       │   │
│  │  - 项目特定配置                                        │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 5.3 接口适配层实现

#### 5.3.1 ILogAdapter接口

```csharp
namespace Basement.Tools.Bridge
{
    public interface ILogAdapter
    {
        void Debug(string message, string tag = null);
        void Info(string message, string tag = null);
        void Warning(string message, string tag = null);
        void Error(string message, string tag = null);
        void Fatal(string message, string tag = null);
        void Exception(Exception exception, string tag = null);

        void SetLogLevel(LogLevel level);
        LogLevel GetLogLevel();
    }
}
```

#### 5.3.2 LogBridge实现

```csharp
using Basement.Logging;

namespace Basement.Tools.Bridge
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
            var logManagerObj = new GameObject("[LogManager]");
            _logManager = logManagerObj.AddComponent<LogManager>();
            UnityEngine.Object.DontDestroyOnLoad(logManagerObj);

            var config = new LogConfig
            {
                DefaultLogLevel = LogLevel.Info,
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

        public void SetLogLevel(LogLevel level)
        {
            _logManager?.SetLogLevel(level);
        }

        public LogLevel GetLogLevel()
        {
            return _logManager?.CurrentLevel ?? LogLevel.Info;
        }
    }
}
```

#### 5.3.3 SingletonAdapter实现

```csharp
using UnityEngine;

namespace Basement.Tools.Bridge
{
    public abstract class SingletonAdapter<T> : MonoBehaviour where T : SingletonAdapter<T>
    {
        protected static volatile T _instance;
        private static bool _isDestroyed = false;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (_isDestroyed)
                {
                    Debug.LogError($"SingletonAdapter<{typeof(T).Name}>不存在，无法获取实例");
                    return null;
                }

                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var existingInstance = FindObjectOfType<T>();
                            if (existingInstance != null)
                            {
                                _instance = existingInstance;
                            }
                            else
                            {
                                var singletonObj = new GameObject($"[Singleton_{typeof(T).Name}]");
                                _instance = singletonObj.AddComponent<T>();
                                DontDestroyOnLoad(singletonObj);
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = (T)this;
                DontDestroyOnLoad(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _isDestroyed = true;
            }
        }
    }
}
```

### 5.4 依赖注入机制

#### 5.4.1 ServiceLocator实现

```csharp
using System;
using System.Collections.Generic;

namespace Basement.Tools.Bridge
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private static readonly object _lock = new object();

        public static void Register<T>(T service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            lock (_lock)
            {
                _services[typeof(T)] = service;
            }
        }

        public static T Get<T>()
        {
            lock (_lock)
            {
                if (_services.TryGetValue(typeof(T), out var service))
                {
                    return (T)service;
                }
            }

            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered");
        }

        public static bool TryGet<T>(out T service)
        {
            lock (_lock)
            {
                if (_services.TryGetValue(typeof(T), out var obj))
                {
                    service = (T)obj;
                    return true;
                }
            }

            service = default;
            return false;
        }

        public static void Unregister<T>()
        {
            lock (_lock)
            {
                _services.Remove(typeof(T));
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _services.Clear();
            }
        }
    }
}
```

#### 5.4.2 LogManager重构（支持依赖注入）

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Basement.Tools.Bridge;

namespace Basement.Logging
{
    public class LogManager : SingletonAdapter<LogManager>
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

                ServiceLocator.Register<ILogAdapter>(LogBridge.Instance);
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
```

### 5.5 配置隔离方案

#### 5.5.1 LogConfigProvider

```csharp
using System.IO;
using UnityEngine;

namespace Basement.Tools.Bridge
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
                    Debug.LogError($"加载日志配置失败: {ex.Message}");
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
                Debug.LogError($"保存日志配置失败: {ex.Message}");
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
```

#### 5.5.2 DebugWindow运行时适配

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Basement.Tools.Bridge;

namespace Basement.Logging
{
    public class DebugWindow : SingletonAdapter<DebugWindow>, ILogOutput
    {
        private LogLevel _logLevel = LogLevel.Info;
        private readonly List<string> _logLines = new List<string>();
        private Vector2 _scrollPosition;
        private bool _isVisible = false;
        private bool _autoScroll = true;
        private LogLevel _filterLevel = LogLevel.Debug;
        private string _searchText = "";
        private GUIStyle _logStyle;
        private GUIStyle _debugStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _fatalStyle;
        private GUIStyle _toolbarStyle;
        private GUIStyle _toolbarButtonStyle;
        private int _maxLines = 1000;
        private Rect _windowRect = new Rect(10, 10, 800, 600);

        public int MaxLines
        {
            get { return _maxLines; }
            set { _maxLines = Math.Max(100, value); }
        }

        private void OnGUI()
        {
            if (!_isVisible)
                return;

            if (_logStyle == null)
                InitializeStyles();

            _windowRect = GUILayout.Window(0, _windowRect, DrawDebugWindow, "调试日志");
        }

        private void DrawDebugWindow(int windowId)
        {
            GUILayout.BeginVertical();

            DrawToolbar();
            GUILayout.Space(5);
            DrawSearchBox();
            GUILayout.Space(5);
            DrawLogContent();

            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(_toolbarStyle);

            _autoScroll = GUILayout.Toggle(_autoScroll, "自动滚动", _toolbarButtonStyle, GUILayout.Width(80));
            GUILayout.Space(10);

            string[] levelNames = Enum.GetNames(typeof(LogLevel));
            _filterLevel = (LogLevel)GUILayout.Toolbar((int)_filterLevel, levelNames, _toolbarButtonStyle, GUILayout.Width(300));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("清空", _toolbarButtonStyle, GUILayout.Width(60)))
                ClearLogs();

            if (GUILayout.Button("关闭", _toolbarButtonStyle, GUILayout.Width(60)))
                _isVisible = false;

            GUILayout.EndHorizontal();
        }

        private void DrawSearchBox()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", GUILayout.Width(50));
            _searchText = GUILayout.TextField(_searchText);
            GUILayout.EndHorizontal();
        }

        private void DrawLogContent()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);

            foreach (var line in _logLines)
            {
                if (!string.IsNullOrEmpty(_searchText) || !line.Contains(_searchText))
                    continue;

                LogLevel lineLevel = GetLogLevel(line);
                if (lineLevel < _filterLevel)
                    continue;

                GUIStyle style = GetLogStyle(line);
                GUILayout.Label(line, style);
            }

            GUILayout.EndScrollView();

            if (_autoScroll)
                _scrollPosition.y = float.MaxValue;
        }

        private GUIStyle GetLogStyle(string logLine)
        {
            if (logLine.Contains("[DEBUG]"))
                return _debugStyle;
            if (logLine.Contains("[INFO]"))
                return _infoStyle;
            if (logLine.Contains("[WARNING]"))
                return _warningStyle;
            if (logLine.Contains("[ERROR]"))
                return _errorStyle;
            if (logLine.Contains("[FATAL]"))
                return _fatalStyle;
            return _logStyle;
        }

        private LogLevel GetLogLevel(string logLine)
        {
            if (logLine.Contains("[DEBUG]"))
                return LogLevel.Debug;
            if (logLine.Contains("[INFO]"))
                return LogLevel.Info;
            if (logLine.Contains("[WARNING]"))
                return LogLevel.Warning;
            if (logLine.Contains("[ERROR]"))
                return LogLevel.Error;
            if (logLine.Contains("[FATAL]"))
                return LogLevel.Fatal;
            return LogLevel.Debug;
        }

        private void InitializeStyles()
        {
            _logStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                wordWrap = true,
                fontSize = 12,
                padding = new RectOffset(5, 2, 5, 2),
                margin = new RectOffset(2, 1, 2, 1)
            };

            _debugStyle = new GUIStyle(_logStyle)
            {
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };

            _infoStyle = new GUIStyle(_logStyle)
            {
                normal = { textColor = Color.white }
            };

            _warningStyle = new GUIStyle(_logStyle)
            {
                normal = { textColor = Color.yellow }
            };

            _errorStyle = new GUIStyle(_logStyle)
            {
                normal = { textColor = Color.red }
            };

            _fatalStyle = new GUIStyle(_logStyle)
            {
                normal = { textColor = Color.magenta }
            };

            _toolbarStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(2, 2, new Color(0.3f, 0.3f, 0.3f)) },
                padding = new RectOffset(5, 5, 5, 5),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _toolbarButtonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(10, 10, 5, 5),
                fontSize = 11
            };
        }

        private Texture2D MakeTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void ClearLogs()
        {
            _logLines.Clear();
            _scrollPosition = Vector2.zero;
        }

        public void Log(string message, LogLevel level, string tag = null)
        {
            if (level < _logLevel)
                return;

            _logLines.Add(message);

            if (_logLines.Count > _maxLines)
                _logLines.RemoveAt(0);
        }

        public void SetLogLevel(LogLevel level)
        {
            _logLevel = level;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
                _isVisible = !_isVisible;
        }

        public void ToggleWindow()
        {
            _isVisible = !_isVisible;
        }

        public void ShowWindow()
        {
            _isVisible = true;
        }

        public void HideWindow()
        {
            _isVisible = false;
        }
    }
}
```

---

## 6. 实施优先级建议

### 6.1 短期目标（1-2周）

| 优先级 | 任务 | 预计工时 | 风险 |
|--------|------|----------|------|
| **P0** | 创建Bridge文件夹结构 | 1小时 | 低 |
| **P0** | 实现ILogAdapter接口 | 1小时 | 低 |
| **P0** | 实现LogBridge | 2小时 | 低 |
| **P0** | 实现SingletonAdapter | 2小时 | 低 |
| **P0** | 实现ServiceLocator | 2小时 | 低 |
| **P1** | 重构LogManager使用SingletonAdapter | 3小时 | 中 |
| **P1** | 实现LogConfigProvider | 2小时 | 低 |

### 6.2 中期目标（3-4周）

| 优先级 | 任务 | 预计工时 | 风险 |
|--------|------|----------|------|
| **P1** | 重构DebugWindow移除编辑器依赖 | 4小时 | 中 |
| **P1** | 更新现有代码使用LogBridge | 4小时 | 中 |
| **P2** | 创建迁移文档和指南 | 4小时 | 低 |
| **P2** | 编写单元测试 | 6小时 | 中 |

### 6.3 长期目标（5-8周）

| 优先级 | 任务 | 预计工时 | 风险 |
|--------|------|----------|------|
| **P2** | 重构测试脚本移除业务依赖 | 8小时 | 高 |
| **P2** | 设计通用调试工具框架 | 12小时 | 高 |
| **P3** | 完善文档和示例代码 | 8小时 | 低 |
| **P3** | 性能优化和压力测试 | 8小时 | 中 |

### 6.4 实施路线图

```
第1周：
├── 创建Bridge文件夹结构
├── 实现核心接口和适配器
└── 重构LogManager

第2周：
├── 实现ServiceLocator
├── 实现LogConfigProvider
└── 更新现有代码使用LogBridge

第3-4周：
├── 重构DebugWindow
├── 创建迁移文档
└── 编写单元测试

第5-8周：
├── 重构测试脚本
├── 设计通用调试工具框架
├── 完善文档
└── 性能优化
```

---

## 7. 总结

### 7.1 关键发现

1. **核心日志系统设计优秀**：接口清晰、扩展性强、易于使用
2. **存在耦合问题**：依赖Singleton基类和编辑器API
3. **测试脚本与业务强耦合**：难以直接迁移
4. **迁移难度中等**：核心功能可快速迁移，但需要处理依赖

### 7.2 建议方案

1. **立即实施**：创建桥接层，解耦Tools模块
2. **逐步重构**：优先处理核心功能，再处理测试脚本
3. **文档先行**：创建详细的迁移指南和使用文档
4. **持续优化**：根据实际使用情况不断改进

### 7.3 预期收益

1. **降低耦合度**：Tools模块可独立迁移到其他项目
2. **提高可维护性**：清晰的分层架构便于维护和扩展
3. **增强灵活性**：支持依赖注入，便于测试和替换
4. **提升开发效率**：统一的接口和配置管理

---

## 8. 附录

### 8.1 术语表

| 术语 | 说明 |
|------|------|
| **Bridge Pattern** | 桥接模式，用于将抽象部分与实现部分分离 |
| **Dependency Injection** | 依赖注入，一种实现控制反转的设计模式 |
| **Service Locator** | 服务定位器，用于定位和获取服务实例 |
| **Singleton** | 单例模式，确保一个类只有一个实例 |

### 8.2 参考资料

- 设计模式：可复用面向对象软件的基础
- Unity官方文档：MonoBehaviour生命周期
- Clean Architecture：软件架构与设计模式

---

**报告生成时间**：2026-02-08
**评估人员**：AI架构师
**版本**：1.0
