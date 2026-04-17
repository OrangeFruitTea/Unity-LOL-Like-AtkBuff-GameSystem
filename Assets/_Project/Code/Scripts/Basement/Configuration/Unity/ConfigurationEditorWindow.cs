#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
        
        // 新增配置文件相关变量
        private string _newFileName = "NewConfig";
        private string _newConfigType = "CharacterConfig";
        private string[] _configTypes = { "CharacterConfig" };

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

            // 新增配置文件
            EditorGUILayout.LabelField("新增配置文件", EditorStyles.boldLabel);
            _newFileName = EditorGUILayout.TextField("文件名称", _newFileName);
            _newConfigType = EditorGUILayout.Popup("配置类型", Array.IndexOf(_configTypes, _newConfigType), _configTypes).ToString();
            
            if (GUILayout.Button("创建新配置文件"))
            {
                CreateNewConfigFile();
            }

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

        /// <summary>
        /// 创建新的配置文件
        /// </summary>
        private void CreateNewConfigFile()
        {
            try
            {
                // 验证文件名
                if (string.IsNullOrEmpty(_newFileName))
                {
                    _errorMessage = "文件名不能为空";
                    return;
                }

                // 过滤非法字符
                string sanitizedFileName = Regex.Replace(_newFileName, "[\\/:*?\"<>|]", "");
                if (sanitizedFileName != _newFileName)
                {
                    _newFileName = sanitizedFileName;
                    _errorMessage = "文件名包含非法字符，已自动过滤";
                }

                // 确保配置目录存在
                string configDir = Path.Combine(Application.streamingAssetsPath, "Config");
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                // 构建文件路径
                string filePath = Path.Combine(configDir, sanitizedFileName + ".json");

                // 检查文件是否已存在
                if (File.Exists(filePath))
                {
                    _errorMessage = "文件已存在，请使用其他文件名";
                    return;
                }

                // 根据配置类型生成默认模板
                JObject defaultTemplate = GenerateDefaultTemplate(_newConfigType);

                // 保存新文件
                string jsonContent = defaultTemplate.ToString(Formatting.Indented);
                File.WriteAllText(filePath, jsonContent);

                // 更新配置路径并加载新文件
                _configPath = filePath;
                _jsonData = defaultTemplate;
                _configName = sanitizedFileName;

                _errorMessage = "";
                EditorUtility.DisplayDialog("成功", $"新配置文件创建成功！\n路径: {filePath}", "确定");
            }
            catch (Exception ex)
            {
                _errorMessage = "创建配置文件失败: " + ex.Message;
            }
        }

        /// <summary>
        /// 生成默认配置模板
        /// </summary>
        /// <param name="configType">配置类型</param>
        /// <returns>默认模板</returns>
        private JObject GenerateDefaultTemplate(string configType)
        {
            JObject template = new JObject();

            switch (configType)
            {
                case "CharacterConfig":
                    template["Version"] = "1.0.0";
                    template["Name"] = _newFileName;
                    template["FilePath"] = "";
                    template["LastModified"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                    template["CharacterId"] = _newFileName;
                    template["DisplayName"] = _newFileName;
                    template["MaxHealth"] = 100;
                    template["AttackPower"] = 10;
                    template["MoveSpeed"] = 5.0f;
                    template["Skills"] = new JArray();
                    template["Stats"] = new JObject();
                    break;
            }

            return template;
        }
    }
}
#endif