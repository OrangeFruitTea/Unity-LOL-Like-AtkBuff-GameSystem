using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Basement.Json
{
    public class FileJsonStorage : IJsonStorage
    {
        private readonly string _rootPath;
        private readonly IJsonSerializer _serializer;
        private readonly string _fileExtension;

        public FileJsonStorage(string rootPath, IJsonSerializer serializer, string fileExtension = ".json")
        {
            _rootPath = rootPath;
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _fileExtension = fileExtension;

            if (!Directory.Exists(_rootPath))
            {
                Directory.CreateDirectory(_rootPath);
            }
        }

        private string GetFilePath(string key)
        {
            string safeKey = key.Replace("/", "_").Replace("\\", "_").Replace(":", "_");
            return Path.Combine(_rootPath, $"{safeKey}{_fileExtension}");
        }

        public void Save<T>(string key, T data)
        {
            try
            {
                string filePath = GetFilePath(key);
                string directory = Path.GetDirectoryName(filePath);
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = _serializer.Serialize(data);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"保存JSON文件失败 [Key: {key}]: {ex.Message}");
            }
        }

        public T Load<T>(string key)
        {
            try
            {
                string filePath = GetFilePath(key);
                
                if (!File.Exists(filePath))
                {
                    return default;
                }

                string json = File.ReadAllText(filePath);
                return _serializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载JSON文件失败 [Key: {key}]: {ex.Message}");
                return default;
            }
        }

        public bool Exists(string key)
        {
            string filePath = GetFilePath(key);
            return File.Exists(filePath);
        }

        public void Delete(string key)
        {
            try
            {
                string filePath = GetFilePath(key);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除JSON文件失败 [Key: {key}]: {ex.Message}");
            }
        }

        public void Clear()
        {
            try
            {
                if (Directory.Exists(_rootPath))
                {
                    Directory.Delete(_rootPath, true);
                    Directory.CreateDirectory(_rootPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"清空JSON存储失败: {ex.Message}");
            }
        }

        public IEnumerable<string> GetAllKeys()
        {
            if (!Directory.Exists(_rootPath))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.GetFiles(_rootPath, $"*{_fileExtension}", SearchOption.AllDirectories)
                .Select(filePath => 
                {
                    string relativePath = filePath.Substring(_rootPath.Length).TrimStart(Path.DirectorySeparatorChar);
                    return relativePath.Substring(0, relativePath.Length - _fileExtension.Length).Replace("_", "/");
                });
        }

        public async Task SaveAsync<T>(string key, T data)
        {
            try
            {
                string filePath = GetFilePath(key);
                string directory = Path.GetDirectoryName(filePath);
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = _serializer.Serialize(data);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"异步保存JSON文件失败 [Key: {key}]: {ex.Message}");
            }
        }

        public async Task<T> LoadAsync<T>(string key)
        {
            try
            {
                string filePath = GetFilePath(key);
                
                if (!File.Exists(filePath))
                {
                    return default;
                }

                string json = await File.ReadAllTextAsync(filePath);
                return _serializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"异步加载JSON文件失败 [Key: {key}]: {ex.Message}");
                return default;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            string filePath = GetFilePath(key);
            return await Task.Run(() => File.Exists(filePath));
        }

        public async Task DeleteAsync(string key)
        {
            try
            {
                string filePath = GetFilePath(key);
                
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"异步删除JSON文件失败 [Key: {key}]: {ex.Message}");
            }
        }

        public async Task ClearAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(_rootPath))
                    {
                        Directory.Delete(_rootPath, true);
                        Directory.CreateDirectory(_rootPath);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"异步清空JSON存储失败: {ex.Message}");
            }
        }
    }
}
