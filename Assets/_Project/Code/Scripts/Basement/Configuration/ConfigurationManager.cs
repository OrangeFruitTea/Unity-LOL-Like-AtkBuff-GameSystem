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