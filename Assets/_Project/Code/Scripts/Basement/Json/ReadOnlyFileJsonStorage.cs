using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Basement.Json
{
    /// <summary>
    /// 只读文件存储（如 StreamingAssets 下游戏内容表）。写入操作会抛出异常。
    /// </summary>
    public sealed class ReadOnlyFileJsonStorage : IJsonStorage
    {
        private readonly string _rootPath;
        private readonly IJsonSerializer _serializer;
        private readonly string _fileExtension;
        private readonly IJsonLog _log;

        public ReadOnlyFileJsonStorage(
            string rootPath,
            IJsonSerializer serializer,
            string fileExtension = ".json",
            IJsonLog log = null)
        {
            _rootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _fileExtension = fileExtension;
            _log = log ?? new DebugJsonLog();
        }

        private string GetFilePath(string key) =>
            JsonStoragePathHelper.GetFilePath(_rootPath, key, _fileExtension);

        public T Load<T>(string key)
        {
            try
            {
                string filePath = GetFilePath(key);
                if (!File.Exists(filePath))
                    return default;

                string json = File.ReadAllText(filePath);
                if (!_serializer.TryDeserialize(json, out T value, out var err))
                {
                    _log.LogError($"反序列化失败 [Key: {key}]: {err}", nameof(ReadOnlyFileJsonStorage));
                    return default;
                }

                return value;
            }
            catch (Exception ex)
            {
                _log.LogError($"加载JSON文件失败 [Key: {key}]: {ex.Message}", nameof(ReadOnlyFileJsonStorage));
                return default;
            }
        }

        public bool Exists(string key) => File.Exists(GetFilePath(key));

        public IEnumerable<string> GetAllKeys()
        {
            if (!Directory.Exists(_rootPath))
                return Enumerable.Empty<string>();

            return Directory.GetFiles(_rootPath, $"*{_fileExtension}", SearchOption.AllDirectories)
                .Select(filePath =>
                {
                    string relativePath = filePath.Substring(_rootPath.Length).TrimStart(Path.DirectorySeparatorChar);
                    return relativePath.Substring(0, relativePath.Length - _fileExtension.Length).Replace("_", "/");
                });
        }

        public void Save<T>(string key, T data) =>
            throw new NotSupportedException($"{nameof(ReadOnlyFileJsonStorage)} 不支持写入。");

        public void Delete(string key) =>
            throw new NotSupportedException($"{nameof(ReadOnlyFileJsonStorage)} 不支持删除。");

        public void Clear() =>
            throw new NotSupportedException($"{nameof(ReadOnlyFileJsonStorage)} 不支持清空。");

        public Task SaveAsync<T>(string key, T data) =>
            throw new NotSupportedException($"{nameof(ReadOnlyFileJsonStorage)} 不支持写入。");

        public async Task<T> LoadAsync<T>(string key)
        {
            try
            {
                string filePath = GetFilePath(key);
                if (!File.Exists(filePath))
                    return default;

                string json = await File.ReadAllTextAsync(filePath);
                if (!_serializer.TryDeserialize(json, out T value, out var err))
                {
                    _log.LogError($"异步反序列化失败 [Key: {key}]: {err}", nameof(ReadOnlyFileJsonStorage));
                    return default;
                }

                return value;
            }
            catch (Exception ex)
            {
                _log.LogError($"异步加载JSON文件失败 [Key: {key}]: {ex.Message}", nameof(ReadOnlyFileJsonStorage));
                return default;
            }
        }

        public Task<bool> ExistsAsync(string key) =>
            Task.FromResult(Exists(key));

        public Task DeleteAsync(string key) =>
            throw new NotSupportedException($"{nameof(ReadOnlyFileJsonStorage)} 不支持删除。");

        public Task ClearAsync() =>
            throw new NotSupportedException($"{nameof(ReadOnlyFileJsonStorage)} 不支持清空。");
    }
}
