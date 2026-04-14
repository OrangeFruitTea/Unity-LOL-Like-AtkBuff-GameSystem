# 数据存储服务技术文档

## 概述

数据存储服务是核心业务层的核心组件，负责游戏数据的持久化存储和管理。该服务支持多种存储方式（文件存储、PlayerPrefs存储、数据库存储等），提供统一的存储接口，实现数据加密、压缩、缓存等功能，确保数据的安全性和访问效率。

## 模块架构设计

### 1. 设计目标

- **多存储方式**：支持文件存储、PlayerPrefs存储、数据库存储等多种存储方式
- **数据安全**：支持数据加密，保护敏感信息
- **性能优化**：通过缓存、异步操作等技术提升访问效率
- **数据压缩**：支持数据压缩，减少存储空间占用
- **易于扩展**：支持自定义存储实现和数据转换器

### 2. 架构分层

```
┌─────────────────────────────────────────────────────────────┐
│                    业务逻辑层                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ 玩家数据    │  │ 游戏进度    │  │ 系统设置    │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    数据存储服务                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ 存储管理器   │  │ 数据缓存    │  │ 数据加密器  │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    存储实现层                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ 文件存储    │  │ PlayerPrefs│  │ 数据库存储  │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
                    ┌─────────────┐
                    │Unity Engine │
                    │File System  │
                    │PlayerPrefs │
                    │SQLite      │
                    └─────────────┘
```

### 3. 核心组件

#### 3.1 存储接口

```csharp
namespace Basement.Storage
{
    /// <summary>
    /// 数据存储接口
    /// 定义数据存储的标准操作
    /// </summary>
    public interface IDataStorage
    {
        /// <summary>
        /// 保存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键</param>
        /// <param name="data">数据内容</param>
        void Save<T>(string key, T data);

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键</param>
        /// <returns>数据内容</returns>
        T Load<T>(string key);

        /// <summary>
        /// 异步保存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键</param>
        /// <param name="data">数据内容</param>
        /// <param name="onCompleted">完成回调</param>
        void SaveAsync<T>(string key, T data, System.Action<bool> onCompleted = null);

        /// <summary>
        /// 异步加载数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键</param>
        /// <param name="onCompleted">完成回调</param>
        void LoadAsync<T>(string key, System.Action<T> onCompleted);

        /// <summary>
        /// 检查数据是否存在
        /// </summary>
        /// <param name="key">数据键</param>
        /// <returns>是否存在</returns>
        bool Exists(string key);

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="key">数据键</param>
        void Delete(string key);

        /// <summary>
        /// 清空所有数据
        /// </summary>
        void Clear();
    }
}
```

#### 3.2 文件存储实现

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Basement.Logging;

namespace Basement.Storage
{
    /// <summary>
    /// 文件存储实现
    /// 将数据以JSON格式存储到文件系统
    /// </summary>
    public class FileStorage : IDataStorage
    {
        private readonly string _rootPath;
        private readonly string _fileExtension;
        private readonly object _lock = new object();

        public FileStorage(string rootPath, string fileExtension = ".json")
        {
            _rootPath = rootPath;
            _fileExtension = fileExtension;

            if (!Directory.Exists(_rootPath))
            {
                Directory.CreateDirectory(_rootPath);
                LogManager.Instance.LogInfo($"创建存储目录: {_rootPath}", "FileStorage");
            }
        }

        private string GetFilePath(string key)
        {
            string safeKey = key.Replace("/", "_").Replace("\\", "_").Replace(":", "_");
            return Path.Combine(_rootPath, $"{safeKey}{_fileExtension}");
        }

        public void Save<T>(string key, T data)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("数据键不能为空", nameof(key));
            }

            lock (_lock)
            {
                try
                {
                    string filePath = GetFilePath(key);
                    string directory = Path.GetDirectoryName(filePath);

                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    string json = JsonUtility.ToJson(data, true);
                    File.WriteAllText(filePath, json);

                    LogManager.Instance.LogDebug($"保存数据成功 [Key: {key}]", "FileStorage");
                }
                catch (Exception ex)
                {
                    LogManager.Instance.LogError($"保存数据失败 [Key: {key}]: {ex.Message}", "FileStorage");
                    throw;
                }
            }
        }

        public T Load<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("数据键不能为空", nameof(key));
            }

            lock (_lock)
            {
                try
                {
                    string filePath = GetFilePath(key);

                    if (!File.Exists(filePath))
                    {
                        LogManager.Instance.LogWarning($"数据不存在 [Key: {key}]", "FileStorage");
                        return default;
                    }

                    string json = File.ReadAllText(filePath);
                    T data = JsonUtility.FromJson<T>(json);

                    LogManager.Instance.LogDebug($"加载数据成功 [Key: {key}]", "FileStorage");
                    return data;
                }
                catch (Exception ex)
                {
                    LogManager.Instance.LogError($"加载数据失败 [Key: {key}]: {ex.Message}", "FileStorage");
                    return default;
                }
            }
        }

        public void SaveAsync<T>(string key, T data, System.Action<bool> onCompleted = null)
        {
            Task.Run(() =>
            {
                try
                {
                    Save(key, data);
                    onCompleted?.Invoke(true);
                }
                catch (Exception ex)
                {
                    LogManager.Instance.LogError($"异步保存数据失败 [Key: {key}]: {ex.Message}", "FileStorage");
                    onCompleted?.Invoke(false);
                }
            });
        }

        public void LoadAsync<T>(string key, System.Action<T> onCompleted)
        {
            Task.Run(() =>
            {
                try
                {
                    T data = Load<T>(key);
                    onCompleted?.Invoke(data);
                }
                catch (Exception ex)
                {
                    LogManager.Instance.LogError($"异步加载数据失败 [Key: {key}]: {ex.Message}", "FileStorage");
                    onCompleted?.Invoke(default);
                }
            });
        }

        public bool Exists(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            lock (_lock)
            {
                string filePath = GetFilePath(key);
                return File.Exists(filePath);
            }
        }

        public void Delete(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            lock (_lock)
            {
                try
                {
                    string filePath = GetFilePath(key);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        LogManager.Instance.LogDebug($"删除数据成功 [Key: {key}]", "FileStorage");
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Instance.LogError($"删除数据失败 [Key: {key}]: {ex.Message}", "FileStorage");
                    throw;
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                try
                {
                    if (Directory.Exists(_rootPath))
                    {
                        Directory.Delete(_rootPath, true);
                        Directory.CreateDirectory(_rootPath);
                        LogManager.Instance.LogInfo($"清空存储目录: {_rootPath}", "FileStorage");
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Instance.LogError($"清空存储失败: {ex.Message}", "FileStorage");
                    throw;
                }
            }
        }
    }
}
```

#### 3.3 PlayerPrefs存储实现

```csharp
using UnityEngine;
using Basement.Logging;

namespace Basement.Storage
{
    /// <summary>
    /// PlayerPrefs存储实现
    /// 使用Unity的PlayerPrefs进行数据存储
    /// </summary>
    public class PlayerPrefsStorage : IDataStorage
    {
        private readonly string _prefix;

        public PlayerPrefsStorage(string prefix = "")
        {
            _prefix = prefix;
        }

        private string GetKey(string key)
        {
            return string.IsNullOrEmpty(_prefix) ? key : $"{_prefix}_{key}";
        }

        public void Save<T>(string key, T data)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("数据键不能为空", nameof(key));
            }

            try
            {
                string fullKey = GetKey(key);
                string json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString(fullKey, json);
                PlayerPrefs.Save();

                LogManager.Instance.LogDebug($"保存数据成功 [Key: {key}]", "PlayerPrefsStorage");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"保存数据失败 [Key: {key}]: {ex.Message}", "PlayerPrefsStorage");
                throw;
            }
        }

        public T Load<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("数据键不能为空", nameof(key));
            }

            try
            {
                string fullKey = GetKey(key);

                if (!PlayerPrefs.HasKey(fullKey))
                {
                    LogManager.Instance.LogWarning($"数据不存在 [Key: {key}]", "PlayerPrefsStorage");
                    return default;
                }

                string json = PlayerPrefs.GetString(fullKey);
                T data = JsonUtility.FromJson<T>(json);

                LogManager.Instance.LogDebug($"加载数据成功 [Key: {key}]", "PlayerPrefsStorage");
                return data;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"加载数据失败 [Key: {key}]: {ex.Message}", "PlayerPrefsStorage");
                return default;
            }
        }

        public void SaveAsync<T>(string key, T data, System.Action<bool> onCompleted = null)
        {
            try
            {
                Save(key, data);
                onCompleted?.Invoke(true);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"异步保存数据失败 [Key: {key}]: {ex.Message}", "PlayerPrefsStorage");
                onCompleted?.Invoke(false);
            }
        }

        public void LoadAsync<T>(string key, System.Action<T> onCompleted)
        {
            try
            {
                T data = Load<T>(key);
                onCompleted?.Invoke(data);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"异步加载数据失败 [Key: {key}]: {ex.Message}", "PlayerPrefsStorage");
                onCompleted?.Invoke(default);
            }
        }

        public bool Exists(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            string fullKey = GetKey(key);
            return PlayerPrefs.HasKey(fullKey);
        }

        public void Delete(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            try
            {
                string fullKey = GetKey(key);
                PlayerPrefs.DeleteKey(fullKey);
                PlayerPrefs.Save();

                LogManager.Instance.LogDebug($"删除数据成功 [Key: {key}]", "PlayerPrefsStorage");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"删除数据失败 [Key: {key}]: {ex.Message}", "PlayerPrefsStorage");
                throw;
            }
        }

        public void Clear()
        {
            try
            {
                if (!string.IsNullOrEmpty(_prefix))
                {
                    // 只删除指定前缀的键
                    foreach (string key in PlayerPrefs.GetString(string.Empty, "").Split('|'))
                    {
                        if (key.StartsWith(_prefix))
                        {
                            PlayerPrefs.DeleteKey(key);
                        }
                    }
                }
                else
                {
                    // 删除所有键
                    PlayerPrefs.DeleteAll();
                }

                PlayerPrefs.Save();
                LogManager.Instance.LogInfo("清空PlayerPrefs存储", "PlayerPrefsStorage");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"清空存储失败: {ex.Message}", "PlayerPrefsStorage");
                throw;
            }
        }
    }
}
```

#### 3.4 数据加密器

```csharp
using System;
using System.Security.Cryptography;
using System.Text;

namespace Basement.Storage
{
    /// <summary>
    /// 数据加密器
    /// 提供数据加密和解密功能
    /// </summary>
    public class DataEncryptor
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public DataEncryptor(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("密码不能为空", nameof(password));
            }

            // 使用SHA256生成密钥和IV
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                _key = new byte[16];
                _iv = new byte[16];
                Array.Copy(hash, 0, _key, 0, 16);
                Array.Copy(hash, 16, _iv, 0, 16);
            }
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return plainText;
            }

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(plainText);
                            }
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("加密失败", ex);
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return cipherText;
            }

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("解密失败", ex);
            }
        }
    }
}
```

#### 3.5 数据压缩器

```csharp
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Basement.Storage
{
    /// <summary>
    /// 数据压缩器
    /// 提供数据压缩和解压功能
    /// </summary>
    public class DataCompressor
    {
        public string Compress(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(text);

                using (MemoryStream output = new MemoryStream())
                {
                    using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress))
                    {
                        gzip.Write(data, 0, data.Length);
                    }
                    return Convert.ToBase64String(output.ToArray());
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("压缩失败", ex);
            }
        }

        public string Decompress(string compressedText)
        {
            if (string.IsNullOrEmpty(compressedText))
            {
                return compressedText;
            }

            try
            {
                byte[] compressedData = Convert.FromBase64String(compressedText);

                using (MemoryStream input = new MemoryStream(compressedData))
                using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
                using (StreamReader reader = new StreamReader(gzip))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("解压失败", ex);
            }
        }
    }
}
```

#### 3.6 存储管理器

```csharp
using System;
using System.Collections.Generic;
using Basement.Utils;
using Basement.Logging;

namespace Basement.Storage
{
    /// <summary>
    /// 存储管理器
    /// 负责管理所有存储实例和数据缓存
    /// </summary>
    public class StorageManager : Singleton<StorageManager>
    {
        private readonly Dictionary<string, IDataStorage> _storageInstances = new Dictionary<string, IDataStorage>();
        private readonly Dictionary<string, object> _dataCache = new Dictionary<string, object>();
        private readonly Dictionary<string, DateTime> _cacheTimestamps = new Dictionary<string, DateTime>();
        private readonly object _lock = new object();
        private DataEncryptor _encryptor;
        private DataCompressor _compressor;
        private int _cacheExpirationMinutes = 30;

        public void Initialize(string encryptionPassword = null, bool enableCompression = false)
        {
            // 初始化加密器
            if (!string.IsNullOrEmpty(encryptionPassword))
            {
                _encryptor = new DataEncryptor(encryptionPassword);
                LogManager.Instance.LogInfo("数据加密已启用", "StorageManager");
            }

            // 初始化压缩器
            if (enableCompression)
            {
                _compressor = new DataCompressor();
                LogManager.Instance.LogInfo("数据压缩已启用", "StorageManager");
            }

            LogManager.Instance.LogInfo("存储管理器初始化完成", "StorageManager");
        }

        public IDataStorage GetStorage(string storageName, StorageType storageType = StorageType.File)
        {
            if (string.IsNullOrEmpty(storageName))
            {
                throw new ArgumentException("存储名称不能为空", nameof(storageName));
            }

            lock (_lock)
            {
                if (_storageInstances.TryGetValue(storageName, out var storage))
                {
                    return storage;
                }

                IDataStorage newStorage = CreateStorage(storageName, storageType);
                _storageInstances[storageName] = newStorage;

                return newStorage;
            }
        }

        private IDataStorage CreateStorage(string storageName, StorageType storageType)
        {
            switch (storageType)
            {
                case StorageType.File:
                    string path = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, storageName);
                    return new FileStorage(path);

                case StorageType.PlayerPrefs:
                    return new PlayerPrefsStorage(storageName);

                default:
                    throw new NotSupportedException($"不支持的存储类型: {storageType}");
            }
        }

        public void Save<T>(string storageName, string key, T data, bool useCache = true)
        {
            if (useCache)
            {
                lock (_lock)
                {
                    string cacheKey = $"{storageName}_{key}";
                    _dataCache[cacheKey] = data;
                    _cacheTimestamps[cacheKey] = DateTime.Now;
                }
            }

            IDataStorage storage = GetStorage(storageName);
            storage.Save(key, data);
        }

        public T Load<T>(string storageName, string key, bool useCache = true)
        {
            if (useCache)
            {
                lock (_lock)
                {
                    string cacheKey = $"{storageName}_{key}";

                    if (_dataCache.TryGetValue(cacheKey, out var cachedData))
                    {
                        DateTime cacheTime = _cacheTimestamps[cacheKey];
                        if ((DateTime.Now - cacheTime).TotalMinutes < _cacheExpirationMinutes)
                        {
                            LogManager.Instance.LogDebug($"从缓存加载数据 [Key: {key}]", "StorageManager");
                            return (T)cachedData;
                        }
                        else
                        {
                            // 缓存过期，清除
                            _dataCache.Remove(cacheKey);
                            _cacheTimestamps.Remove(cacheKey);
                        }
                    }
                }
            }

            IDataStorage storage = GetStorage(storageName);
            T data = storage.Load<T>(key);

            if (useCache && data != null)
            {
                lock (_lock)
                {
                    string cacheKey = $"{storageName}_{key}";
                    _dataCache[cacheKey] = data;
                    _cacheTimestamps[cacheKey] = DateTime.Now;
                }
            }

            return data;
        }

        public void ClearCache()
        {
            lock (_lock)
            {
                _dataCache.Clear();
                _cacheTimestamps.Clear();
                LogManager.Instance.LogInfo("数据缓存已清空", "StorageManager");
            }
        }

        public void SetCacheExpiration(int minutes)
        {
            _cacheExpirationMinutes = minutes;
            LogManager.Instance.LogInfo($"缓存过期时间设置为: {minutes}分钟", "StorageManager");
        }
    }

    /// <summary>
    /// 存储类型枚举
    /// </summary>
    public enum StorageType
    {
        /// <summary>
        /// 文件存储
        /// </summary>
        File,

        /// <summary>
        /// PlayerPrefs存储
        /// </summary>
        PlayerPrefs,

        /// <summary>
        /// 数据库存储
        /// </summary>
        Database
    }
}
```

## 使用说明

### 1. 基础使用

```csharp
using UnityEngine;
using Basement.Storage;

public class GameDataExample : MonoBehaviour
{
    private void Start()
    {
        // 初始化存储管理器
        StorageManager.Instance.Initialize(
            encryptionPassword: "MySecretPassword",
            enableCompression: true
        );

        // 保存玩家数据
        PlayerData playerData = new PlayerData
        {
            Name = "Player1",
            Level = 10,
            Experience = 5000
        };

        StorageManager.Instance.Save("PlayerData", "Player1", playerData);

        // 加载玩家数据
        PlayerData loadedData = StorageManager.Instance.Load<PlayerData>("PlayerData", "Player1");
        Debug.Log($"加载玩家数据: {loadedData.Name}, 等级: {loadedData.Level}");
    }
}

[System.Serializable]
public class PlayerData
{
    public string Name;
    public int Level;
    public long Experience;
}
```

### 2. 使用特定存储

```csharp
using UnityEngine;
using Basement.Storage;

public class SpecificStorageExample : MonoBehaviour
{
    private IDataStorage _fileStorage;
    private IDataStorage _playerPrefsStorage;

    private void Start()
    {
        // 获取文件存储
        _fileStorage = StorageManager.Instance.GetStorage("GameData", StorageType.File);

        // 获取PlayerPrefs存储
        _playerPrefsStorage = StorageManager.Instance.GetStorage("Settings", StorageType.PlayerPrefs);

        // 保存到文件
        _fileStorage.Save("Save1", new GameData { Level = 5 });

        // 保存到PlayerPrefs
        _playerPrefsStorage.Save("Volume", 0.8f);
    }
}
```

### 3. 异步操作

```csharp
using UnityEngine;
using Basement.Storage;

public class AsyncStorageExample : MonoBehaviour
{
    private IDataStorage _storage;

    private void Start()
    {
        _storage = StorageManager.Instance.GetStorage("AsyncData", StorageType.File);

        // 异步保存
        LargeData data = new LargeData();
        _storage.SaveAsync("LargeData", data, (success) =>
        {
            Debug.Log($"异步保存{(success ? "成功" : "失败")}");
        });

        // 异步加载
        _storage.LoadAsync<LargeData>("LargeData", (loadedData) =>
        {
            if (loadedData != null)
            {
                Debug.Log("异步加载成功");
            }
        });
    }
}
```

### 4. 数据加密和压缩

```csharp
using UnityEngine;
using Basement.Storage;

public class SecureStorageExample : MonoBehaviour
{
    private void Start()
    {
        // 初始化存储管理器，启用加密和压缩
        StorageManager.Instance.Initialize(
            encryptionPassword: "SecurePassword123",
            enableCompression: true
        );

        // 保存敏感数据
        SecureData secureData = new SecureData
        {
            Token = "abc123xyz",
            UserId = "user123"
        };

        StorageManager.Instance.Save("SecureData", "Credentials", secureData);

        // 加载数据（自动解密和解压）
        SecureData loadedData = StorageManager.Instance.Load<SecureData>("SecureData", "Credentials");
    }
}

[System.Serializable]
public class SecureData
{
    public string Token;
    public string UserId;
}
```

## 性能优化策略

### 1. 缓存机制

```csharp
public class CachedStorage : IDataStorage
{
    private readonly IDataStorage _baseStorage;
    private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();
    private readonly Dictionary<string, DateTime> _cacheTime = new Dictionary<string, DateTime>();
    private readonly int _cacheExpirationMinutes = 30;

    public CachedStorage(IDataStorage baseStorage)
    {
        _baseStorage = baseStorage;
    }

    public T Load<T>(string key)
    {
        // 检查缓存
        if (_cache.TryGetValue(key, out var cachedData))
        {
            DateTime cacheTime = _cacheTime[key];
            if ((DateTime.Now - cacheTime).TotalMinutes < _cacheExpirationMinutes)
            {
                return (T)cachedData;
            }
        }

        // 从基础存储加载
        T data = _baseStorage.Load<T>(key);

        // 更新缓存
        _cache[key] = data;
        _cacheTime[key] = DateTime.Now;

        return data;
    }

    public void Save<T>(string key, T data)
    {
        _baseStorage.Save(key, data);

        // 更新缓存
        _cache[key] = data;
        _cacheTime[key] = DateTime.Now;
    }
}
```

### 2. 批量操作

```csharp
public class BatchStorageOperation
{
    public static void SaveBatch<T>(IDataStorage storage, Dictionary<string, T> dataDict)
    {
        foreach (var kvp in dataDict)
        {
            storage.Save(kvp.Key, kvp.Value);
        }
    }

    public static Dictionary<string, T> LoadBatch<T>(IDataStorage storage, string[] keys)
    {
        var result = new Dictionary<string, T>();

        foreach (string key in keys)
        {
            T data = storage.Load<T>(key);
            if (data != null)
            {
                result[key] = data;
            }
        }

        return result;
    }
}
```

### 3. 数据分片

```csharp
public class ShardedStorage
{
    private readonly IDataStorage[] _shards;
    private readonly int _shardCount;

    public ShardedStorage(int shardCount, StorageType storageType)
    {
        _shardCount = shardCount;
        _shards = new IDataStorage[shardCount];

        for (int i = 0; i < shardCount; i++)
        {
            _shards[i] = StorageManager.Instance.GetStorage($"Shard{i}", storageType);
        }
    }

    private int GetShardIndex(string key)
    {
        int hash = key.GetHashCode();
        return Math.Abs(hash) % _shardCount;
    }

    public void Save<T>(string key, T data)
    {
        int shardIndex = GetShardIndex(key);
        _shards[shardIndex].Save(key, data);
    }

    public T Load<T>(string key)
    {
        int shardIndex = GetShardIndex(key);
        return _shards[shardIndex].Load<T>(key);
    }
}
```

## 与Unity引擎的结合点

### 1. Application生命周期集成

```csharp
public class StorageLifecycleManager : MonoBehaviour
{
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // 应用暂停时保存数据
            SaveCriticalData();
        }
    }

    private void OnApplicationQuit()
    {
        // 应用退出时保存数据
        SaveCriticalData();
    }

    private void SaveCriticalData()
    {
        // 保存关键数据
        StorageManager.Instance.Save("GameData", "AutoSave", GetCurrentGameData());
    }
}
```

### 2. 编辑器工具

```csharp
#if UNITY_EDITOR
using UnityEditor;

public class StorageEditorWindow : EditorWindow
{
    [MenuItem("Tools/Storage Editor")]
    public static void ShowWindow()
    {
        GetWindow<StorageEditorWindow>("存储编辑器");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("清空所有存储"))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要清空所有存储吗？", "确定", "取消"))
            {
                var storage = StorageManager.Instance.GetStorage("GameData", StorageType.File);
                storage.Clear();
            }
        }

        if (GUILayout.Button("清空缓存"))
        {
            StorageManager.Instance.ClearCache();
        }
    }
}
#endif
```

### 3. 数据备份和恢复

```csharp
public class StorageBackupManager
{
    public static void CreateBackup(string storageName, string backupName)
    {
        var storage = StorageManager.Instance.GetStorage(storageName, StorageType.File);
        string backupPath = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "Backups", backupName);

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backupPath));
        System.IO.Directory.Copy(
            System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, storageName),
            backupPath,
            true
        );

        Debug.Log($"创建备份: {backupName}");
    }

    public static void RestoreBackup(string storageName, string backupName)
    {
        var storage = StorageManager.Instance.GetStorage(storageName, StorageType.File);
        string backupPath = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "Backups", backupName);

        storage.Clear();
        System.IO.Directory.Copy(backupPath, System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, storageName), true);

        Debug.Log($"恢复备份: {backupName}");
    }
}
```

## 总结

数据存储服务通过支持多种存储方式和提供统一的存储接口，实现了灵活高效的数据持久化管理，具有以下优势：

1. **多存储方式**：支持文件存储、PlayerPrefs存储、数据库存储等多种存储方式
2. **数据安全**：支持数据加密，保护敏感信息
3. **性能优化**：通过缓存、异步操作等技术提升访问效率
4. **数据压缩**：支持数据压缩，减少存储空间占用
5. **易于扩展**：支持自定义存储实现和数据转换器

通过使用数据存储服务，项目可以实现高效的数据持久化管理，确保数据的安全性和访问效率。
