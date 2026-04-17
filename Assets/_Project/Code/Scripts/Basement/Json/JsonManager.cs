using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Basement.Utils;

namespace Basement.Json
{
    public class JsonManager : Singleton<JsonManager>
    {
        public const string StorageNameGameContent = "GameContent";

        private Dictionary<string, IJsonStorage> _storageSystems = new Dictionary<string, IJsonStorage>();
        private IJsonSerializer _defaultSerializer;
        private IJsonSerializer _gameContentSerializer;
        private IJsonStorage _defaultStorage;
        private bool _isInitialized = false;

        public IJsonSerializer DefaultSerializer => _defaultSerializer;

        /// <summary> 只读游戏内容表推荐序列化器（无 TypeNameHandling.Auto）。 </summary>
        public IJsonSerializer GameContentSerializer => _gameContentSerializer;

        public IJsonStorage DefaultStorage => _defaultStorage;

        /// <summary> StreamingAssets 根目录下的只读存储（按 key 存取，与 <see cref="FileJsonStorage"/> 相同安全文件名规则）。 </summary>
        public IJsonStorage GameContentStorage => GetStorage(StorageNameGameContent);

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("JsonManager已经初始化，跳过重复初始化");
                return;
            }

            try
            {
                _defaultSerializer = new NewtonsoftJsonSerializer(JsonSerializationProfiles.CreateRuntimePersistenceSettings());
                _gameContentSerializer = new NewtonsoftJsonSerializer(JsonSerializationProfiles.CreateGameContentSettings());

                string persistentDataPath = Path.Combine(Application.persistentDataPath, "JsonData");
                var fileStorage = new FileJsonStorage(persistentDataPath, _defaultSerializer);
                RegisterStorage("File", fileStorage);

                var playerPrefsStorage = new PlayerPrefsJsonStorage(_defaultSerializer);
                RegisterStorage("PlayerPrefs", playerPrefsStorage);

                string streamingRoot = Application.streamingAssetsPath;
                if (!Directory.Exists(streamingRoot))
                    Debug.LogWarning($"[JsonManager] StreamingAssets 目录不存在: {streamingRoot}");
                var gameContentStorage = new ReadOnlyFileJsonStorage(streamingRoot, _gameContentSerializer);
                RegisterStorage(StorageNameGameContent, gameContentStorage);

                _defaultStorage = fileStorage;

                _isInitialized = true;
                Debug.Log("JsonManager初始化完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"JsonManager初始化失败: {ex.Message}");
            }
        }

        public void RegisterStorage(string name, IJsonStorage storage)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("存储系统名称不能为空");
                return;
            }

            if (storage == null)
            {
                Debug.LogError($"存储系统 [{name}] 不能为null");
                return;
            }

            if (_storageSystems.ContainsKey(name))
            {
                Debug.LogWarning($"存储系统 [{name}] 已存在，将被覆盖");
                _storageSystems[name] = storage;
            }
            else
            {
                _storageSystems.Add(name, storage);
            }

            Debug.Log($"注册存储系统: {name}");
        }

        public IJsonStorage GetStorage(string name)
        {
            if (_storageSystems.TryGetValue(name, out var storage))
            {
                return storage;
            }

            Debug.LogWarning($"未找到存储系统: {name}");
            return null;
        }

        public void SetDefaultStorage(string name)
        {
            var storage = GetStorage(name);
            
            if (storage != null)
            {
                _defaultStorage = storage;
                Debug.Log($"设置默认存储系统: {name}");
            }
        }

        public void Save<T>(string key, T data)
        {
            _defaultStorage?.Save(key, data);
        }

        public void Save<T>(string key, T data, string storageName)
        {
            var storage = GetStorage(storageName);
            storage?.Save(key, data);
        }

        public T Load<T>(string key)
        {
            return _defaultStorage != null ? _defaultStorage.Load<T>(key) : default;
        }

        public T Load<T>(string key, string storageName)
        {
            var storage = GetStorage(storageName);
            return storage != null ? storage.Load<T>(key) : default;
        }

        public bool Exists(string key)
        {
            return _defaultStorage?.Exists(key) ?? false;
        }

        public bool Exists(string key, string storageName)
        {
            var storage = GetStorage(storageName);
            return storage?.Exists(key) ?? false;
        }

        public void Delete(string key)
        {
            _defaultStorage?.Delete(key);
        }

        public void Delete(string key, string storageName)
        {
            var storage = GetStorage(storageName);
            storage?.Delete(key);
        }

        public void Clear()
        {
            _defaultStorage?.Clear();
        }

        public void Clear(string storageName)
        {
            var storage = GetStorage(storageName);
            storage?.Clear();
        }

        public string Serialize<T>(T obj)
        {
            return _defaultSerializer?.Serialize(obj);
        }

        public string Serialize<T>(T obj, JsonSerializerProfile profile)
        {
            var serializer = GetSerializer(profile);
            return serializer?.Serialize(obj);
        }

        public T Deserialize<T>(string json)
        {
            return _defaultSerializer != null ? _defaultSerializer.Deserialize<T>(json) : default;
        }

        public T Deserialize<T>(string json, JsonSerializerProfile profile)
        {
            var serializer = GetSerializer(profile);
            return serializer != null ? serializer.Deserialize<T>(json) : default;
        }

        public object Deserialize(string json, Type type)
        {
            return _defaultSerializer?.Deserialize(json, type);
        }

        public IJsonSerializer GetSerializer(JsonSerializerProfile profile)
        {
            return profile == JsonSerializerProfile.GameContent ? _gameContentSerializer : _defaultSerializer;
        }

        public JsonReadResult<T> DeserializeWithResult<T>(string json, JsonSerializerProfile profile = JsonSerializerProfile.RuntimePersistence)
        {
            if (json == null)
                return JsonReadResult<T>.Fail("json is null");
            var serializer = GetSerializer(profile);
            if (serializer == null)
                return JsonReadResult<T>.Fail("serializer not initialized");
            if (!serializer.TryDeserialize(json, out T value, out var err))
                return JsonReadResult<T>.Fail(err);
            return JsonReadResult<T>.Ok(value);
        }

        public bool TryDeserialize<T>(string json, JsonSerializerProfile profile, out T value, out string error)
        {
            value = default;
            error = null;
            if (json == null)
            {
                error = "json is null";
                return false;
            }

            var serializer = GetSerializer(profile);
            if (serializer == null)
            {
                error = "serializer not initialized";
                return false;
            }

            return serializer.TryDeserialize(json, out value, out error);
        }

        /// <summary> 从绝对路径读取 UTF-8 文本并反序列化（不经过 <see cref="IJsonStorage"/>）。 </summary>
        public JsonReadResult<T> DeserializeFromFilePath<T>(string filePath, JsonSerializerProfile profile = JsonSerializerProfile.GameContent)
        {
            if (string.IsNullOrEmpty(filePath))
                return JsonReadResult<T>.Fail("filePath is null or empty");
            if (!File.Exists(filePath))
                return JsonReadResult<T>.Fail($"file not found: {filePath}");

            string json;
            try
            {
                json = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                return JsonReadResult<T>.Fail(ex.Message);
            }

            return DeserializeWithResult<T>(json, profile);
        }

        /// <summary> 相对 <see cref="Application.streamingAssetsPath"/> 的路径，使用 <see cref="Path.Combine"/> 拼接。 </summary>
        public JsonReadResult<T> DeserializeFromStreamingAssetsRelative<T>(string relativePath, JsonSerializerProfile profile = JsonSerializerProfile.GameContent)
        {
            if (string.IsNullOrEmpty(relativePath))
                return JsonReadResult<T>.Fail("relativePath is empty");
            string full = Path.Combine(Application.streamingAssetsPath, relativePath);
            return DeserializeFromFilePath<T>(full, profile);
        }

        public byte[] SerializeToBytes<T>(T obj)
        {
            return _defaultSerializer?.SerializeToBytes(obj);
        }

        public T DeserializeFromBytes<T>(byte[] bytes)
        {
            return _defaultSerializer != null ? _defaultSerializer.DeserializeFromBytes<T>(bytes) : default;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _storageSystems.Clear();
            _defaultSerializer = null;
            _gameContentSerializer = null;
            _defaultStorage = null;
            _isInitialized = false;
        }
    }
}
