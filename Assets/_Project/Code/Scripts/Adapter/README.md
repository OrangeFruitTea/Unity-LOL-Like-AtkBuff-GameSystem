# Adapter模块系统

## 概述

Adapter模块系统是一个用于解耦项目各模块间依赖关系的桥接层架构，提供统一的接口适配和桥接实现，确保模块间的低耦合性和高可迁移性。

## 目录结构

```
Adapter/
├── Interfaces/            # 适配器接口定义
│   └── ILogAdapter.cs
├── Bridge/                # 桥接实现
│   ├── LogBridge.cs
│   ├── SingletonAdapter.cs
│   └── LogConfigProvider.cs
├── Implementations/       # 具体实现
├── Examples/              # 使用示例
│   └── LogBridgeUsageExample.cs
├── Tests/                 # 测试脚本
│   └── LogAdapterTest.cs
├── LogLevel.cs            # 日志级别枚举
├── LogConfig.cs           # 日志配置类
├── README.md              # 本文件
└── MIGRATION_GUIDE.md     # 迁移指南
```

## 架构设计

### 设计原则

1. **低耦合**：模块间通过接口通信，减少直接依赖
2. **高内聚**：每个模块职责明确，内部结构清晰
3. **可迁移**：核心功能可独立迁移到其他项目
4. **可扩展**：支持自定义实现和扩展
5. **接口兼容**：保持接口稳定性，确保现有功能不受影响

### 核心组件

#### 1. **接口层 (Interfaces)**

- **ILogAdapter**：统一的日志操作接口，定义了标准化的日志方法

#### 2. **桥接层 (Bridge)**

- **LogBridge**：实现ILogAdapter接口，桥接底层LogManager
- **SingletonAdapter**：提供单例模式的通用实现
- **LogConfigProvider**：管理日志配置的加载和保存

#### 3. **基础组件**

- **LogLevel**：日志级别枚举
- **LogConfig**：日志配置类

## 快速开始

### 1. 基础使用

```csharp
using Adapter;
using Adapter.Bridge;

// 获取日志实例
var logger = LogBridge.Instance;

// 记录不同级别的日志
logger.Debug("调试信息", "MyModule");
logger.Info("普通信息", "MyModule");
logger.Warning("警告信息", "MyModule");
logger.Error("错误信息", "MyModule");
logger.Fatal("致命错误", "MyModule");
```

### 2. 异常处理

```csharp
using Adapter;
using Adapter.Bridge;

try
{
    // 你的代码
}
catch (System.Exception ex)
{
    LogBridge.Instance.Exception(ex, "MyModule");
}
```

### 3. 日志级别控制

```csharp
using Adapter;
using Adapter.Bridge;

var logger = LogBridge.Instance;

// 设置日志级别
logger.SetLogLevel(LogLevel.Warning);

// 获取当前日志级别
LogLevel currentLevel = logger.GetLogLevel();
```

### 4. 接口模式使用

```csharp
using Adapter.Interfaces;
using Adapter.Bridge;

// 通过接口引用
ILogAdapter logger = LogBridge.Instance;
logger.Info("通过接口调用", "MyModule");
```

## 配置管理

### 1. 使用默认配置

```csharp
using Adapter.Bridge;

var config = LogConfigProvider.GetDefaultConfig();
// 使用默认配置
```

### 2. 从文件加载配置

```csharp
using Adapter.Bridge;

var config = LogConfigProvider.LoadConfig();
// 使用加载的配置
```

### 3. 保存配置到文件

```csharp
using Adapter;
using Adapter.Bridge;

var config = new LogConfig
{
    DefaultLogLevel = LogLevel.Debug,
    EnableConsoleOutput = true,
    EnableFileOutput = true,
    EnableDebugWindow = true
};

LogConfigProvider.SaveConfig(config);
```

## 优势

1. **解耦**：模块间通过接口通信，减少直接依赖
2. **可迁移**：核心功能可独立迁移到其他项目
3. **统一接口**：提供标准化的操作接口
4. **灵活性**：支持自定义实现和扩展
5. **兼容性**：保持接口稳定，确保现有功能不受影响

## 最佳实践

1. **使用标签**：为不同模块使用不同的标签，便于过滤和查找
2. **合理设置日志级别**：开发环境使用Debug级别，生产环境使用Info或Warning级别
3. **异常处理**：使用Exception方法记录异常，包含完整的堆栈信息
4. **配置管理**：将配置保存在文件中，便于在不同环境间切换
5. **接口编程**：优先使用接口引用，而不是具体实现

## 常见问题

### Q1: 如何禁用日志输出？

A: 将日志级别设置为Fatal，这样只有Fatal级别的日志才会输出。

```csharp
LogBridge.Instance.SetLogLevel(LogLevel.Fatal);
```

### Q2: 如何添加自定义日志输出？

A: 实现ILogAdapter接口，创建自定义的适配器实现。

### Q3: 如何在不同环境使用不同配置？

A: 使用LogConfigProvider.LoadConfig()从不同配置文件加载配置。

### Q4: 如何在单元测试中使用？

A: 直接使用LogBridge.Instance，或创建ILogAdapter的mock实现。

## 更多示例

查看`Examples/LogBridgeUsageExample.cs`获取更多使用示例。

## 迁移指南

请参考`MIGRATION_GUIDE.md`文件，了解如何从旧的日志系统迁移到新的Adapter模块系统。
