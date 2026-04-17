using System;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Basement.Json
{
    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public NewtonsoftJsonSerializer()
            : this(JsonSerializationProfiles.CreateRuntimePersistenceSettings())
        {
        }

        public NewtonsoftJsonSerializer(JsonSerializerSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public string Serialize<T>(T obj)
        {
            try
            {
                return JsonConvert.SerializeObject(obj, _settings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"JSON序列化失败: {ex.Message}");
                return null;
            }
        }

        public T Deserialize<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json, _settings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"JSON反序列化失败: {ex.Message}");
                return default;
            }
        }

        public object Deserialize(string json, Type type)
        {
            try
            {
                return JsonConvert.DeserializeObject(json, type, _settings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"JSON反序列化失败: {ex.Message}");
                return null;
            }
        }

        public byte[] SerializeToBytes<T>(T obj)
        {
            try
            {
                string json = Serialize(obj);
                return Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"JSON序列化为字节数组失败: {ex.Message}");
                return null;
            }
        }

        public T DeserializeFromBytes<T>(byte[] bytes)
        {
            try
            {
                string json = Encoding.UTF8.GetString(bytes);
                return Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"从字节数组反序列化JSON失败: {ex.Message}");
                return default;
            }
        }

        public bool TryDeserialize<T>(string json, out T value, out string error)
        {
            value = default;
            error = null;
            if (json == null)
            {
                error = "json is null";
                return false;
            }

            try
            {
                value = JsonConvert.DeserializeObject<T>(json, _settings);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryDeserialize(string json, Type type, out object value, out string error)
        {
            value = null;
            error = null;
            if (json == null)
            {
                error = "json is null";
                return false;
            }

            if (type == null)
            {
                error = "type is null";
                return false;
            }

            try
            {
                value = JsonConvert.DeserializeObject(json, type, _settings);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
