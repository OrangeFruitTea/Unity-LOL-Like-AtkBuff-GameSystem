# 配置解析模块技术文档

## 概述

配置解析模块是基础设施层的核心组件，负责游戏配置文件的加载、解析、验证和管理。该模块专注于JSON配置格式，提供统一的配置访问接口，实现配置的热更新和版本管理，确保游戏配置的灵活性和可维护性。同时提供Unity环境下的可视化JSON信息编辑器，方便用户通过界面编辑和管理配置。

## 模块架构设计

### 1. 设计目标

- **JSON格式专注**：专注于JSON配置格式，提供高效的解析和管理
- **类型安全**：提供强类型的配置访问接口，减少运行时错误
- **热更新支持**：支持运行时配置更新，无需重启游戏
- **版本管理**：支持配置版本控制和迁移
- **性能优化**：高效的配置加载和解析，减少启动时间
- **可视化编辑**：提供Unity环境下的JSON信息可视化编辑器，支持键值对的增删改查

### 2. 架构分层

```
┌─────────────────────────────────────────────────────────────┐
│                    业务逻辑层                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ 角色配置    │  │ 技能配置    │  │ 关卡配置    │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    配置管理层                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ 配置管理器   │  │ 配置缓存    │  │ 配置验证器  │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    配置解析层                               │
│  ┌─────────────┐                                          │
│  │ JSON解析器  │                                          │
│  └─────────────┘                                          │
└─────────────────────────────────────────────────────────────┘
                           ↓
                    ┌─────────────┐
                    │ Newtonsoft  │
                    │ Json.NET    │
                    │ System.Text │
                    │ .Json       │
                    └─────────────┘
```

### 3. 核心组件

#### 3.1 配置接口

```csharp
namespace Basement.Configuration
{
    /// <summary>
    /// 配置接口
    /// 定义配置的基本属性和行为
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// 配置版本
        /// </summary>
        string Version { get; set; }

        /// <summary>
        /// 配置名称
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// 配置文件路径
        /// </summary>
        string FilePath { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        System.DateTime LastModified { get; set; }

        /// <summary>
        /// 验证配置
        /// </summary>
        /// <returns>验证结果</returns>
        bool Validate();

        /// <summary>
        /// 获取验证错误信息
        /// </summary>
        /// <returns>错误信息列表</returns>
        System.Collections.Generic.List<string> GetValidationErrors();
    }
}
```

#### 3.2 配置解析器接口

```csharp
namespace Basement.Configuration
{
    /// <summary>
    /// 配置解析器接口
    /// 定义配置解析的标准操作
    /// </summary>
    public interface IConfigurationParser
    {
        /// <summary>
        /// 支持的文件扩展名
        /// </summary>
        string[] SupportedExtensions { get; }

        /// <summary>
        /// 解析配置文件
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <returns>解析的配置对象</returns>
        T Parse<T>(string filePath) where T : IConfiguration, new();

        /// <summary>
        /// 从字符串解析配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="content">配置内容</param>
        /// <returns>解析的配置对象</returns>
        T ParseFromString<T>(string content) where T : IConfiguration, new();

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="config">配置对象</param>
        /// <param name="filePath">文件路径</param>
        void Save<T>(T config, string filePath) where T : IConfiguration;

        /// <summary>
        /// 将配置转换为字符串
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="config">配置对象</param>
        /// <returns>配置字符串</returns>
        string ToString<T>(T config) where T : IConfiguration;
    }
}
```

#### 3.3 JSON配置解析器

```csharp
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Basement.Configuration
{
    /// <summary>
    /// JSON配置解析器
    /// 使用Newtonsoft.Json实现JSON格式的配置解析
    /// </summary>
    public class JsonConfigurationParser : IConfigurationParser
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public string[] SupportedExtensions => new[] { ".json" };

        public JsonConfigurationParser()
        {
            _serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }

        public T Parse<T>(string filePath) where T : IConfiguration, new()
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"配置文件不存在: {filePath}");
            }

            string content = File.ReadAllText(filePath);
            return ParseFromString<T>(content);
        }

        public T ParseFromString<T>(string content) where T : IConfiguration, new()
        {
            try
            {
                T config = JsonConvert.DeserializeObject<T>(content, _serializerSettings);
                config.FilePath = string.IsNullOrEmpty(config.FilePath) ? "Unknown" : config.FilePath;
                config.LastModified = DateTime.Now;
                return config;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"JSON解析失败: {ex.Message}", ex);
            }
        }

        public void Save<T>(T config, string filePath) where T : IConfiguration
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            string content = ToString(config);
            File.WriteAllText(filePath, content);
            config.FilePath = filePath;
            config.LastModified = File.GetLastWriteTime(filePath);
        }

        public string ToString<T>(T config) where T : IConfiguration
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return JsonConvert.SerializeObject(config, _serializerSettings);
        }

        /// <summary>
        /// 合并JSON配置
        /// </summary>
        /// <param name="baseJson">基础JSON</param>
        /// <param name="overrideJson">覆盖JSON</param>
        /// <returns>合并后的JSON</returns>
        public string MergeJson(string baseJson, string overrideJson)
        {
            JObject baseObj = JObject.Parse(baseJson);
            JObject overrideObj = JObject.Parse(overrideJson);

            MergeJsonObjects(baseObj, overrideObj);

            return baseObj.ToString(Formatting.Indented);
        }

        private void MergeJsonObjects(JObject baseObj, JObject overrideObj)
        {
            foreach (var property in overrideObj.Properties())
            {
                if (baseObj[property.Name] is JObject baseProperty && property.Value is JObject overrideProperty)
                {
                    MergeJsonObjects(baseProperty, overrideProperty);
                }
                else
                {
                    baseObj[property.Name] = property.Value;
                }
            }
        }
    }
}
```



#### 3.5 配置管理器

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Basement.Utils;

namespace Basement.Configuration
{
    /// <summary>
    /// 配置管理器
    /// 负责管理所有配置的加载、缓存和更新
    /// </summary>
    public class ConfigurationManager : Singleton<ConfigurationManager>
    {
        private readonly Dictionary<string, IConfiguration> _configCache = new Dictionary<string, IConfiguration>();
        private readonly Dictionary<string, IConfigurationParser> _parsers = new Dictionary<string, IConfigurationParser>();
        private readonly object _lock = new object();
        private readonly string _configRootPath;

        public ConfigurationManager()
        {
            // 注册默认解析器
            RegisterParser(new JsonConfigurationParser());

            // 设置配置根路径
            _configRootPath = Path.Combine(UnityEngine.Application.streamingAssetsPath, "Config");

            // 确保配置目录存在
            if (!Directory.Exists(_configRootPath))
            {
                Directory.CreateDirectory(_configRootPath);
            }
        }

        /// <summary>
        /// 注册配置解析器
        /// </summary>
        /// <param name="parser">解析器</param>
        public void RegisterParser(IConfigurationParser parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            foreach (string extension in parser.SupportedExtensions)
            {
                _parsers[extension.ToLower()] = parser;
            }
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="configName">配置名称（不含扩展名）</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>配置对象</returns>
        public T LoadConfig<T>(string configName, bool useCache = true) where T : IConfiguration, new()
        {
            string configKey = typeof(T).Name + "_" + configName;

            lock (_lock)
            {
                // 检查缓存
                if (useCache && _configCache.TryGetValue(configKey, out var cachedConfig))
                {
                    return (T)cachedConfig;
                }

                // 查找配置文件
                string configPath = FindConfigFile(configName);
                if (string.IsNullOrEmpty(configPath))
                {
                    throw new FileNotFoundException($"找不到配置文件: {configName}");
                }

                // 解析配置
                string extension = Path.GetExtension(configPath).ToLower();
                if (!_parsers.TryGetValue(extension, out var parser))
                {
                    throw new NotSupportedException($"不支持的配置文件格式: {extension}");
                }

                T config = parser.Parse<T>(configPath);

                // 验证配置
                if (!config.Validate())
                {
                    var errors = config.GetValidationErrors();
                    throw new InvalidOperationException($"配置验证失败: {string.Join(", ", errors)}");
                }

                // 缓存配置
                if (useCache)
                {
                    _configCache[configKey] = config;
                }

                return config;
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="configName">配置名称</param>
        /// <returns>配置对象</returns>
        public T ReloadConfig<T>(string configName) where T : IConfiguration, new()
        {
            string configKey = typeof(T).Name + "_" + configName;

            lock (_lock)
            {
                // 清除缓存
                _configCache.Remove(configKey);

                // 重新加载
                return LoadConfig<T>(configName, useCache: true);
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="config">配置对象</param>
        /// <param name="configName">配置名称</param>
        public void SaveConfig<T>(T config, string configName) where T : IConfiguration
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            string configPath = Path.Combine(_configRootPath, configName + GetConfigExtension<T>());

            // 获取解析器
            string extension = Path.GetExtension(configPath).ToLower();
            if (!_parsers.TryGetValue(extension, out var parser))
            {
                throw new NotSupportedException($"不支持的配置文件格式: {extension}");
            }

            // 保存配置
            parser.Save(config, configPath);

            // 更新缓存
            string configKey = typeof(T).Name + "_" + configName;
            lock (_lock)
            {
                _configCache[configKey] = config;
            }
        }

        /// <summary>
        /// 清除配置缓存
        /// </summary>
        public void ClearCache()
        {
            lock (_lock)
            {
                _configCache.Clear();
            }
        }

        /// <summary>
        /// 清除指定配置的缓存
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="configName">配置名称</param>
        public void ClearCache<T>(string configName)
        {
            string configKey = typeof(T).Name + "_" + configName;
            lock (_lock)
            {
                _configCache.Remove(configKey);
            }
        }

        /// <summary>
        /// 查找配置文件
        /// </summary>
        /// <param name="configName">配置名称</param>
        /// <returns>配置文件路径</returns>
        private string FindConfigFile(string configName)
        {
            // 按优先级查找配置文件
            string[] searchPaths = new[]
            {
                Path.Combine(_configRootPath, configName + ".json"),
                Path.Combine(UnityEngine.Application.persistentDataPath, "Config", configName + ".json")
            };

            foreach (string path in searchPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取配置扩展名
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <returns>扩展名</returns>
        private string GetConfigExtension<T>() where T : IConfiguration
        {
            // 默认使用JSON格式
            return ".json";
        }
    }
}
```

#### 3.6 配置验证器

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Basement.Configuration
{
    /// <summary>
    /// 配置验证器
    /// 提供配置验证功能
    /// </summary>
    public static class ConfigurationValidator
    {
        /// <summary>
        /// 验证配置对象
        /// </summary>
        /// <param name="config">配置对象</param>
        /// <returns>验证结果</returns>
        public static bool Validate(IConfiguration config)
        {
            if (config == null)
            {
                return false;
            }

            var validationContext = new ValidationContext(config);
            var validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(config, validationContext, validationResults, true);

            return isValid;
        }

        /// <summary>
        /// 获取验证错误信息
        /// </summary>
        /// <param name="config">配置对象</param>
        /// <returns>错误信息列表</returns>
        public static List<string> GetValidationErrors(IConfiguration config)
        {
            var errors = new List<string>();

            if (config == null)
            {
                errors.Add("配置对象为空");
                return errors;
            }

            var validationContext = new ValidationContext(config);
            var validationResults = new List<ValidationResult>();

            Validator.TryValidateObject(config, validationContext, validationResults, true);

            foreach (var result in validationResults)
            {
                errors.Add($"{result.MemberNames.FirstOrDefault()}: {result.ErrorMessage}");
            }

            return errors;
        }
    }
}
```

#### 3.7 配置编辑器

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;

namespace Basement.Configuration.Unity
{
    /// <summary>
    /// 配置编辑器
    /// 提供可视化的JSON配置编辑界面和类型化配置管理
    /// </summary>
    public class ConfigurationEditorWindow : EditorWindow
    {
        private string _configPath;
        private JObject _jsonData;
        private Vector2 _scrollPosition;
        private string _newKey = "";
        private string _newValue = "";
        private string _selectedPath = "";
        private bool _autoSave = false;
        private string _errorMessage = "";
        private string _configName = "Character";

        [MenuItem("Tools/Configuration Editor")]
        public static void ShowWindow()
        {
            GetWindow<ConfigurationEditorWindow>("配置编辑器");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            // 配置管理
            EditorGUILayout.LabelField("配置管理", EditorStyles.boldLabel);
            _configName = EditorGUILayout.TextField("配置名称", _configName);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("从管理器加载"))
            {
                LoadFromManager();
            }
            if (GUILayout.Button("保存到管理器"))
            {
                SaveToManager();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            // 文件操作
            EditorGUILayout.LabelField("文件操作", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _configPath = EditorGUILayout.TextField("配置文件路径", _configPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("选择JSON配置文件", Application.streamingAssetsPath + "/Config", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    _configPath = path;
                    LoadJsonFile(path);
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 自动保存选项
            _autoSave = EditorGUILayout.Toggle("自动保存", _autoSave);

            GUILayout.Space(10);

            // 错误信息显示
            if (!string.IsNullOrEmpty(_errorMessage))
            {
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
                GUILayout.Space(10);
            }

            // 加载/保存按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("加载"))
            {
                if (!string.IsNullOrEmpty(_configPath))
                {
                    LoadJsonFile(_configPath);
                }
                else
                {
                    _errorMessage = "请先选择配置文件路径";
                }
            }
            if (GUILayout.Button("保存"))
            {
                if (!string.IsNullOrEmpty(_configPath) && _jsonData != null)
                {
                    SaveJsonFile(_configPath);
                }
                else
                {
                    _errorMessage = "请先加载配置文件";
                }
            }
            if (GUILayout.Button("清空"))
            {
                _jsonData = new JObject();
                _errorMessage = "";
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);

            // JSON内容编辑
            if (_jsonData != null)
            {
                EditorGUILayout.LabelField("JSON内容编辑", EditorStyles.boldLabel);
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                // 显示JSON结构
                DisplayJsonObject(_jsonData, "");

                // 添加新键值对
                GUILayout.Space(20);
                EditorGUILayout.LabelField("添加新键值对", EditorStyles.boldLabel);
                _newKey = EditorGUILayout.TextField("键", _newKey);
                _newValue = EditorGUILayout.TextField("值", _newValue);
                if (GUILayout.Button("添加"))
                {
                    AddNewKeyValue();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 从配置管理器加载配置
        /// </summary>
        private void LoadFromManager()
        {
            try
            {
                // 这里可以根据需要加载不同类型的配置
                // 示例：加载CharacterConfig
                var config = ConfigurationManager.Instance.LoadConfig<CharacterConfig>(_configName);
                var parser = new JsonConfigurationParser();
                string jsonContent = parser.ToString(config);
                _jsonData = JObject.Parse(jsonContent);
                _errorMessage = "";
                EditorUtility.DisplayDialog("成功", "从配置管理器加载成功", "确定");
            }
            catch (Exception ex)
            {
                _errorMessage = "从配置管理器加载失败: " + ex.Message;
            }
        }

        /// <summary>
        /// 保存配置到配置管理器
        /// </summary>
        private void SaveToManager()
        {
            try
            {
                if (_jsonData != null)
                {
                    // 这里可以根据需要保存不同类型的配置
                    // 示例：保存CharacterConfig
                    var parser = new JsonConfigurationParser();
                    string jsonContent = _jsonData.ToString(Formatting.Indented);
                    var config = parser.ParseFromString<CharacterConfig>(jsonContent);
                    ConfigurationManager.Instance.SaveConfig(config, _configName);
                    _errorMessage = "";
                    EditorUtility.DisplayDialog("成功", "保存到配置管理器成功", "确定");
                }
                else
                {
                    _errorMessage = "请先加载配置文件";
                }
            }
            catch (Exception ex)
            {
                _errorMessage = "保存到配置管理器失败: " + ex.Message;
            }
        }

        /// <summary>
        /// 加载JSON文件
        /// </summary>
        /// <param name="path">文件路径</param>
        private void LoadJsonFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path);
                    _jsonData = JObject.Parse(content);
                    _errorMessage = "";
                    if (_autoSave)
                    {
                        EditorUtility.DisplayDialog("成功", "JSON文件加载成功", "确定");
                    }
                }
                else
                {
                    _errorMessage = "文件不存在";
                }
            }
            catch (JsonException ex)
            {
                _errorMessage = "JSON解析失败: " + ex.Message;
                _jsonData = new JObject();
            }
        }

        /// <summary>
        /// 保存JSON文件
        /// </summary>
        /// <param name="path">文件路径</param>
        private void SaveJsonFile(string path)
        {
            try
            {
                string content = _jsonData.ToString(Formatting.Indented);
                File.WriteAllText(path, content);
                _errorMessage = "";
                if (_autoSave)
                {
                    EditorUtility.DisplayDialog("成功", "JSON文件保存成功", "确定");
                }
            }
            catch (Exception ex)
            {
                _errorMessage = "保存失败: " + ex.Message;
            }
        }

        /// <summary>
        /// 显示JSON对象
        /// </summary>
        /// <param name="obj">JSON对象</param>
        /// <param name="path">当前路径</param>
        private void DisplayJsonObject(JObject obj, string path)
        {
            foreach (var property in obj.Properties())
            {
                string propertyPath = string.IsNullOrEmpty(path) ? property.Name : path + "." + property.Name;
                
                EditorGUILayout.BeginHorizontal();
                
                // 显示键名
                EditorGUILayout.LabelField(property.Name, GUILayout.Width(100));
                
                // 根据值类型显示不同的编辑控件
                if (property.Value is JValue jValue)
                {
                    string currentValue = jValue.ToString();
                    string newValue = EditorGUILayout.TextField(currentValue);
                    if (newValue != currentValue)
                    {
                        property.Value = newValue;
                        if (_autoSave && !string.IsNullOrEmpty(_configPath))
                        {
                            SaveJsonFile(_configPath);
                        }
                    }
                }
                else if (property.Value is JObject nestedObj)
                {
                    EditorGUILayout.LabelField("Object", GUILayout.Width(100));
                    if (GUILayout.Button("展开", GUILayout.Width(60)))
                    {
                        _selectedPath = propertyPath;
                    }
                }
                else if (property.Value is JArray jArray)
                {
                    EditorGUILayout.LabelField($"Array[{jArray.Count}]", GUILayout.Width(100));
                    if (GUILayout.Button("展开", GUILayout.Width(60)))
                    {
                        _selectedPath = propertyPath;
                    }
                }
                
                // 删除按钮
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    property.Remove();
                    if (_autoSave && !string.IsNullOrEmpty(_configPath))
                    {
                        SaveJsonFile(_configPath);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                // 如果是选中的路径，显示嵌套内容
                if (_selectedPath == propertyPath && property.Value is JObject nestedObj2)
                {
                    EditorGUI.indentLevel++;
                    DisplayJsonObject(nestedObj2, propertyPath);
                    EditorGUI.indentLevel--;
                }
            }
        }

        /// <summary>
        /// 添加新的键值对
        /// </summary>
        private void AddNewKeyValue()
        {
            if (!string.IsNullOrEmpty(_newKey))
            {
                try
                {
                    // 尝试解析值为不同类型
                    if (int.TryParse(_newValue, out int intValue))
                    {
                        _jsonData[_newKey] = intValue;
                    }
                    else if (float.TryParse(_newValue, out float floatValue))
                    {
                        _jsonData[_newKey] = floatValue;
                    }
                    else if (bool.TryParse(_newValue, out bool boolValue))
                    {
                        _jsonData[_newKey] = boolValue;
                    }
                    else if (_newValue == "null")
                    {
                        _jsonData[_newKey] = null;
                    }
                    else
                    {
                        // 作为字符串处理
                        _jsonData[_newKey] = _newValue;
                    }
                    
                    _newKey = "";
                    _newValue = "";
                    _errorMessage = "";
                    
                    if (_autoSave && !string.IsNullOrEmpty(_configPath))
                    {
                        SaveJsonFile(_configPath);
                    }
                }
                catch (Exception ex)
                {
                    _errorMessage = "添加键值对失败: " + ex.Message;
                }
            }
            else
            {
                _errorMessage = "键名不能为空";
            }
        }
    }
}
#endif
```

## 使用说明

### 1. 定义配置类

```csharp
using System;
using System.Collections.Generic;
using Basement.Configuration;

namespace GameConfig
{
    /// <summary>
    /// 角色配置
    /// </summary>
    public class CharacterConfig : IConfiguration
    {
        public string Version { get; set; } = "1.0.0";
        public string Name { get; set; }
        public string FilePath { get; set; }
        public DateTime LastModified { get; set; }

        [Required(ErrorMessage = "角色ID不能为空")]
        public string CharacterId { get; set; }

        [Required(ErrorMessage = "角色名称不能为空")]
        public string DisplayName { get; set; }

        [Range(1, 1000, ErrorMessage = "最大生命值必须在1-1000之间")]
        public int MaxHealth { get; set; }

        [Range(1, 500, ErrorMessage = "攻击力必须在1-500之间")]
        public int AttackPower { get; set; }

        [Range(0.1f, 10.0f, ErrorMessage = "移动速度必须在0.1-10.0之间")]
        public float MoveSpeed { get; set; }

        public List<string> Skills { get; set; } = new List<string>();

        public Dictionary<string, int> Stats { get; set; } = new Dictionary<string, int>();

        public bool Validate()
        {
            return ConfigurationValidator.Validate(this);
        }

        public List<string> GetValidationErrors()
        {
            return ConfigurationValidator.GetValidationErrors(this);
        }
    }
}
```

### 2. 加载配置

```csharp
using UnityEngine;
using Basement.Configuration;
using GameConfig;

public class ConfigLoader : MonoBehaviour
{
    private CharacterConfig _characterConfig;

    private void Start()
    {
        // 加载角色配置
        _characterConfig = ConfigurationManager.Instance.LoadConfig<CharacterConfig>("Character");

        Debug.Log($"加载角色配置: {_characterConfig.DisplayName}");
        Debug.Log($"最大生命值: {_characterConfig.MaxHealth}");
        Debug.Log($"攻击力: {_characterConfig.AttackPower}");
    }
}
```

### 3. 保存配置

```csharp
using UnityEngine;
using Basement.Configuration;
using GameConfig;

public class ConfigSaver : MonoBehaviour
{
    public void SaveCharacterConfig(CharacterConfig config)
    {
        // 验证配置
        if (!config.Validate())
        {
            var errors = config.GetValidationErrors();
            Debug.LogError($"配置验证失败: {string.Join(", ", errors)}");
            return;
        }

        // 保存配置
        ConfigurationManager.Instance.SaveConfig(config, "Character");

        Debug.Log("配置保存成功");
    }
}
```

### 4. 热更新配置

```csharp
using UnityEngine;
using Basement.Configuration;
using GameConfig;
using System.IO;

public class ConfigHotUpdater : MonoBehaviour
{
    private CharacterConfig _characterConfig;
    private string _configPath;

    private void Start()
    {
        _characterConfig = ConfigurationManager.Instance.LoadConfig<CharacterConfig>("Character");
        _configPath = Path.Combine(Application.streamingAssetsPath, "Config", "Character.json");

        // 启动配置文件监控
        StartCoroutine(MonitorConfigFile());
    }

    private System.Collections.IEnumerator MonitorConfigFile()
    {
        DateTime lastModified = File.GetLastWriteTime(_configPath);

        while (true)
        {
            yield return new WaitForSeconds(1f);

            DateTime currentModified = File.GetLastWriteTime(_configPath);
            if (currentModified != lastModified)
            {
                Debug.Log("检测到配置文件变化，重新加载配置");

                // 重新加载配置
                _characterConfig = ConfigurationManager.Instance.ReloadConfig<CharacterConfig>("Character");

                // 应用新配置
                ApplyNewConfig();

                lastModified = currentModified;
            }
        }
    }

    private void ApplyNewConfig()
    {
        // 应用新配置到游戏
        Debug.Log($"应用新配置: {_characterConfig.DisplayName}");
    }
}
```

### 5. 配置合并

```csharp
using Basement.Configuration;

public class ConfigMerger
{
    public void MergeConfigs()
    {
        var parser = new JsonConfigurationParser();

        // 基础配置
        string baseConfig = @"
        {
            ""Version"": ""1.0.0"",
            ""Name"": ""BaseConfig"",
            ""MaxHealth"": 100,
            ""AttackPower"": 10
        }";

        // 覆盖配置
        string overrideConfig = @"
        {
            ""MaxHealth"": 150,
            ""MoveSpeed"": 5.0
        }";

        // 合并配置
        string mergedConfig = parser.MergeJson(baseConfig, overrideConfig);

        Debug.Log($"合并后的配置: {mergedConfig}");
    }
}
```

## 性能优化策略

### 1. 配置缓存

```csharp
public class CachedConfigurationManager : ConfigurationManager
{
    private readonly Dictionary<string, (IConfiguration config, DateTime loadTime)> _cacheWithTime =
        new Dictionary<string, (IConfiguration, DateTime)>();

    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public new T LoadConfig<T>(string configName, bool useCache = true) where T : IConfiguration, new()
    {
        string configKey = typeof(T).Name + "_" + configName;

        lock (_cacheWithTime)
        {
            // 检查缓存是否过期
            if (useCache && _cacheWithTime.TryGetValue(configKey, out var cached))
            {
                if (DateTime.Now - cached.loadTime < _cacheExpiration)
                {
                    return (T)cached.config;
                }
                else
                {
                    _cacheWithTime.Remove(configKey);
                }
            }

            // 加载配置
            T config = base.LoadConfig<T>(configName, useCache: false);

            // 缓存配置
            _cacheWithTime[configKey] = (config, DateTime.Now);

            return config;
        }
    }
}
```

### 2. 异步加载

```csharp
using System.Collections;
using UnityEngine;

public class AsyncConfigLoader
{
    public IEnumerator LoadConfigAsync<T>(string configName, System.Action<T> onLoaded) where T : IConfiguration, new()
    {
        // 使用协程实现异步加载
        yield return null;

        T config = ConfigurationManager.Instance.LoadConfig<T>(configName);
        onLoaded?.Invoke(config);
    }

    public void LoadMultipleConfigsAsync(System.Action onComplete)
    {
        StartCoroutine(LoadMultipleConfigsCoroutine(onComplete));
    }

    private IEnumerator LoadMultipleConfigsCoroutine(System.Action onComplete)
    {
        // 加载多个配置
        var characterConfig = ConfigurationManager.Instance.LoadConfig<CharacterConfig>("Character");
        yield return null;

        var skillConfig = ConfigurationManager.Instance.LoadConfig<SkillConfig>("Skill");
        yield return null;

        var itemConfig = ConfigurationManager.Instance.LoadConfig<ItemConfig>("Item");
        yield return null;

        onComplete?.Invoke();
    }
}
```

### 3. 配置预加载

```csharp
public class ConfigPreloader : MonoBehaviour
{
    [SerializeField] private string[] preloadConfigs;

    private IEnumerator Start()
    {
        // 预加载配置
        foreach (string configName in preloadConfigs)
        {
            Debug.Log($"预加载配置: {configName}");
            ConfigurationManager.Instance.LoadConfig<CharacterConfig>(configName);
            yield return null;
        }

        Debug.Log("配置预加载完成");
    }
}
```

### 4. 配置压缩

```csharp
using System.IO;
using System.IO.Compression;

public class CompressedConfigParser : IConfigurationParser
{
    private readonly IConfigurationParser _baseParser;

    public string[] SupportedExtensions => new[] { ".json.gz", ".xml.gz" };

    public CompressedConfigParser(IConfigurationParser baseParser)
    {
        _baseParser = baseParser;
    }

    public T Parse<T>(string filePath) where T : IConfiguration, new()
    {
        string content = DecompressFile(filePath);
        return _baseParser.ParseFromString<T>(content);
    }

    public T ParseFromString<T>(string content) where T : IConfiguration, new()
    {
        byte[] compressedData = Convert.FromBase64String(content);
        string decompressed = Decompress(compressedData);
        return _baseParser.ParseFromString<T>(decompressed);
    }

    public void Save<T>(T config, string filePath) where T : IConfiguration
    {
        string content = _baseParser.ToString(config);
        byte[] compressed = Compress(content);
        File.WriteAllBytes(filePath, compressed);
    }

    public string ToString<T>(T config) where T : IConfiguration
    {
        string content = _baseParser.ToString(config);
        byte[] compressed = Compress(content);
        return Convert.ToBase64String(compressed);
    }

    private string DecompressFile(string filePath)
    {
        byte[] compressedData = File.ReadAllBytes(filePath);
        return Decompress(compressedData);
    }

    private string Decompress(byte[] compressedData)
    {
        using (var compressedStream = new MemoryStream(compressedData))
        using (var decompressor = new GZipStream(compressedStream, CompressionMode.Decompress))
        using (var decompressedStream = new MemoryStream())
        {
            decompressor.CopyTo(decompressedStream);
            return System.Text.Encoding.UTF8.GetString(decompressedStream.ToArray());
        }
    }

    private byte[] Compress(string content)
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes(content);
        using (var compressedStream = new MemoryStream())
        using (var compressor = new GZipStream(compressedStream, CompressionMode.Compress))
        {
            compressor.Write(data, 0, data.Length);
            compressor.Close();
            return compressedStream.ToArray();
        }
    }
}
```

## 与Unity引擎的结合点

### 1. StreamingAssets集成

```csharp
public class StreamingAssetsConfigLoader
{
    public IEnumerator LoadFromStreamingAssets<T>(string configName, System.Action<T> onLoaded) where T : IConfiguration, new()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", configName + ".json");

        // 在Android平台上，StreamingAssets文件需要通过UnityWebRequest加载
        if (Application.platform == RuntimePlatform.Android)
        {
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(filePath))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var parser = new JsonConfigurationParser();
                    T config = parser.ParseFromString<T>(request.downloadHandler.text);
                    onLoaded?.Invoke(config);
                }
                else
                {
                    Debug.LogError($"加载配置失败: {request.error}");
                }
            }
        }
        else
        {
            // 其他平台直接加载
            T config = ConfigurationManager.Instance.LoadConfig<T>(configName);
            onLoaded?.Invoke(config);
        }
    }
}
```

### 2. 编辑器工具

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ConfigurationManager))]
public class ConfigurationManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("验证所有配置"))
        {
            ValidateAllConfigs();
        }

        if (GUILayout.Button("清除配置缓存"))
        {
            ConfigurationManager.Instance.ClearCache();
            Debug.Log("配置缓存已清除");
        }

        if (GUILayout.Button("重新加载所有配置"))
        {
            ReloadAllConfigs();
        }
    }

    private void ValidateAllConfigs()
    {
        // 验证所有配置文件
        string configPath = Path.Combine(Application.streamingAssetsPath, "Config");
        if (Directory.Exists(configPath))
        {
            string[] configFiles = Directory.GetFiles(configPath, "*.json");
            foreach (string file in configFiles)
            {
                try
                {
                    var parser = new JsonConfigurationParser();
                    var config = parser.Parse<CharacterConfig>(file);
                    if (!config.Validate())
                    {
                        Debug.LogWarning($"配置验证失败: {file}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"配置加载失败: {file}, 错误: {ex.Message}");
                }
            }
        }
    }

    private void ReloadAllConfigs()
    {
        ConfigurationManager.Instance.ClearCache();
        Debug.Log("所有配置已重新加载");
    }
}
#endif
```

### 3. 配置可视化编辑器



## 总结

配置解析模块通过专注于JSON配置格式和提供统一的配置访问接口，实现了灵活高效的配置管理，具有以下优势：

1. **JSON格式专注**：专注于JSON配置格式，提供高效的解析和管理
2. **类型安全**：提供强类型的配置访问接口，减少运行时错误
3. **热更新支持**：支持运行时配置更新，无需重启游戏
4. **性能优化**：通过缓存、异步加载等技术提升性能
5. **统一可视化编辑器**：提供Unity环境下的综合配置编辑器，支持JSON键值对的增删改查和类型化配置管理
6. **易于扩展**：支持自定义配置验证规则和编辑器功能

通过使用配置解析模块，项目可以实现灵活的配置管理，提高开发效率和可维护性。特别是统一配置编辑器的实现，整合了JSON可视化编辑和类型化配置管理功能，使得配置文件的编辑更加直观和高效，减少了手动编辑JSON文件的错误率。
