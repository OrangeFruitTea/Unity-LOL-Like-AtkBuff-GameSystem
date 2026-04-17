using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

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