# 迁移指南：从旧日志系统到Adapter模块

## 概述

本指南帮助您将现有代码从直接使用LogManager迁移到使用新的Adapter模块系统。

## 迁移步骤

### 步骤1：备份现有代码

在开始迁移之前，请备份您的项目或使用版本控制系统。

```bash
git commit -m "迁移前备份"
```

### 步骤2：添加命名空间引用

在需要使用日志的文件中，添加以下命名空间：

```csharp
using Adapter;
using Adapter.Bridge;
```

### 步骤3：替换LogManager调用

#### 3.1 替换单例访问

**迁移前：**
```csharp
using Basement.Logging;

LogManager.Instance.LogInfo("信息", "MyModule");
```

**迁移后：**
```csharp
using Adapter;
using Adapter.Bridge;

LogBridge.Instance.Info("信息", "MyModule");
```

#### 3.2 替换日志方法

| 原方法 | 新方法 |
|--------|--------|
| `LogManager.Instance.LogDebug()` | `LogBridge.Instance.Debug()` |
| `LogManager.Instance.LogInfo()` | `LogBridge.Instance.Info()` |
| `LogManager.Instance.LogWarning()` | `LogBridge.Instance.Warning()` |
| `LogManager.Instance.LogError()` | `LogBridge.Instance.Error()` |
| `LogManager.Instance.LogFatal()` | `LogBridge.Instance.Fatal()` |
| `LogManager.Instance.LogException()` | `LogBridge.Instance.Exception()` |

#### 3.3 替换配置方法

**迁移前：**
```csharp
using Basement.Logging;

LogManager.Instance.SetLogLevel(LogLevel.Warning);
LogLevel level = LogManager.Instance.CurrentLevel;
```

**迁移后：**
```csharp
using Adapter;
using Adapter.Bridge;

LogBridge.Instance.SetLogLevel(LogLevel.Warning);
LogLevel level = LogBridge.Instance.GetLogLevel();
```

### 步骤4：更新using语句

移除或注释掉旧的using语句，添加新的using语句：

**迁移前：**
```csharp
using Basement.Logging;
```

**迁移后：**
```csharp
// using Basement.Logging;  // 移除或注释
using Adapter;
using Adapter.Bridge;
```

## 完整迁移示例

### 示例1：FileJsonStorage.cs

**迁移前：**
```csharp
using Basement.Logging;

namespace Basement.Json
{
    public class FileJsonStorage : IJsonStorage
    {
        public void Save<T>(string key, T data)
        {
            try
            {
                // 保存逻辑
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"保存JSON文件失败 [Key: {key}]: {ex.Message}", "FileJsonStorage");
            }
        }
    }
}
```

**迁移后：**
```csharp
using Adapter;
using Adapter.Bridge;

namespace Basement.Json
{
    public class FileJsonStorage : IJsonStorage
    {
        public void Save<T>(string key, T data)
        {
            try
            {
                // 保存逻辑
            }
            catch (Exception ex)
            {
                LogBridge.Instance.Error($"保存JSON文件失败 [Key: {key}]: {ex.Message}", "FileJsonStorage");
            }
        }
    }
}
```

### 示例2：BuffDataLoader.cs

**迁移前：**
```csharp
using Basement.Logging;

public class BuffDataLoader : MonoBehaviour
{
    private void LoadBuffJsonData()
    {
        try
        {
            // 加载逻辑
            LogManager.Instance.LogInfo($"已加载{_buffDataMap.Count}个buff配置", "BuffDataLoader");
        }
        catch (Exception e)
        {
            LogManager.Instance.LogError($"加载buff数据出错：{e.Message}", "BuffDataLoader");
        }
    }
}
```

**迁移后：**
```csharp
using Adapter;
using Adapter.Bridge;

public class BuffDataLoader : MonoBehaviour
{
    private void LoadBuffJsonData()
    {
        try
        {
            // 加载逻辑
            LogBridge.Instance.Info($"已加载{_buffDataMap.Count}个buff配置", "BuffDataLoader");
        }
        catch (Exception e)
        {
            LogBridge.Instance.Error($"加载buff数据出错：{e.Message}", "BuffDataLoader");
        }
    }
}
```

## 批量迁移脚本

如果您需要批量迁移多个文件，可以使用以下PowerShell脚本：

```powershell
# 批量迁移脚本
$files = Get-ChildItem -Path "d:\UnityProject\AtkBuffSystem\Assets\_Project\Code\Scripts" -Recurse -Filter "*.cs"

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw

    # 替换命名空间
    $content = $content -replace 'using Basement\.Logging;', 'using Adapter;\nusing Adapter.Bridge;'

    # 替换方法调用
    $content = $content -replace 'LogManager\.Instance\.LogDebug\(', 'LogBridge.Instance.Debug('
    $content = $content -replace 'LogManager\.Instance\.LogInfo\(', 'LogBridge.Instance.Info('
    $content = $content -replace 'LogManager\.Instance\.LogWarning\(', 'LogBridge.Instance.Warning('
    $content = $content -replace 'LogManager\.Instance\.LogError\(', 'LogBridge.Instance.Error('
    $content = $content -replace 'LogManager\.Instance\.LogFatal\(', 'LogBridge.Instance.Fatal('
    $content = $content -replace 'LogManager\.Instance\.LogException\(', 'LogBridge.Instance.Exception('

    # 替换属性访问
    $content = $content -replace 'LogManager\.Instance\.CurrentLevel', 'LogBridge.Instance.GetLogLevel()'

    Set-Content $file.FullName $content -NoNewline
}

Write-Host "迁移完成！"
```

## 验证迁移

### 1. 编译检查

在Unity中打开项目，确保所有文件都能正常编译。

### 2. 运行测试

运行LogAdapterTest.cs中的测试，确保日志功能正常。

```csharp
// 在场景中添加LogAdapterTest组件
// 运行游戏，按F1执行所有测试
```

### 3. 功能验证

验证以下功能：
- 不同级别的日志是否正常输出
- 日志级别过滤是否生效
- 异常日志是否正常记录
- 现有功能是否不受影响

## 回滚方案

如果迁移后出现问题，可以使用以下方法回滚：

### 方法1：使用版本控制系统

```bash
git checkout HEAD -- .
```

### 方法2：手动回滚

将所有`LogBridge.Instance`替换回`LogManager.Instance`，并恢复旧的using语句。

## 常见问题

### Q1: 迁移后日志不显示？

A: 检查是否正确添加了`using Adapter;`和`using Adapter.Bridge;`命名空间。LogBridge会在第一次访问时自动初始化。

### Q2: 编译错误"找不到LogBridge"？

A: 确保已添加`using Adapter;`和`using Adapter.Bridge;`命名空间。

### Q3: 迁移后性能下降？

A: LogBridge只是LogManager的包装器，性能影响可以忽略不计。如果确实有性能问题，请检查日志级别设置。

### Q4: 如何处理第三方库中的日志调用？

A: 第三方库中的日志调用无法直接迁移。可以考虑：
1. 修改第三方库源码
2. 使用Unity的日志重定向功能
3. 在LogManager中添加对Debug.Log的拦截

## 迁移检查清单

- [ ] 备份项目
- [ ] 添加Adapter命名空间引用
- [ ] 替换所有LogManager.Instance调用
- [ ] 移除旧的using语句
- [ ] 编译检查
- [ ] 运行测试
- [ ] 功能验证

## 迁移后的优势

1. **降低耦合度**：Adapter模块可独立迁移到其他项目
2. **提高可维护性**：清晰的分层架构便于维护和扩展
3. **增强灵活性**：支持自定义实现和扩展
4. **提升开发效率**：统一的接口和配置管理

## 支持

如有问题，请参考完整文档：
- [Adapter模块README](README.md)
- [使用示例](Examples/LogBridgeUsageExample.cs)
- [测试脚本](Tests/LogAdapterTest.cs)
