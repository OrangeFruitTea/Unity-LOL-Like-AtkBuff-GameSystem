using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Basement.Utils;

namespace Basement.Json
{
    public class JsonManager : Singleton<JsonManager>
    {
        private Dictionary<string, IJsonStorage> _storageSystems = new Dictionary<string, IJsonStorage>();
        private IJsonSerializer _defaultSerializer;
        private IJsonStorage _defaultStorage;
        private bool _isInitialized = false;

        public IJsonSerializer DefaultSerializer => _defaultSerializer;
        public IJsonStorage DefaultStorage => _defaultStorage;

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
                _defaultSerializer = new NewtonsoftJsonSerializer();

                string persistentDataPath = Path.Combine(Application.persistentDataPath, "JsonData");
                var fileStorage = new FileJsonStorage(persistentDataPath, _defaultSerializer);
                RegisterStorage("File", fileStorage);

                var playerPrefsStorage = new PlayerPrefsJsonStorage(_defaultSerializer);
                RegisterStorage("PlayerPrefs", playerPrefsStorage);

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

        public T Deserialize<T>(string json)
        {
            return _defaultSerializer != null ? _defaultSerializer.Deserialize<T>(json) : default;
        }

        public object Deserialize(string json, Type type)
        {
            return _defaultSerializer?.Deserialize(json, type);
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
            _defaultStorage = null;
            _isInitialized = false;
        }
    }
}
