using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Basement.Json
{
    public class PlayerPrefsJsonStorage : IJsonStorage
    {
        private readonly IJsonSerializer _serializer;
        private readonly string _prefix;
        private readonly List<string> _trackedKeys;

        public PlayerPrefsJsonStorage(IJsonSerializer serializer, string prefix = "JSON_")
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _prefix = prefix;
            _trackedKeys = new List<string>();
            LoadTrackedKeys();
        }

        private string GetKey(string key)
        {
            return $"{_prefix}{key}";
        }

        private void LoadTrackedKeys()
        {
            string trackedKeysJson = PlayerPrefs.GetString($"{_prefix}_TrackedKeys", "");
            if (!string.IsNullOrEmpty(trackedKeysJson))
            {
                try
                {
                    var keys = _serializer.Deserialize<List<string>>(trackedKeysJson);
                    if (keys != null)
                    {
                        _trackedKeys.AddRange(keys);
                    }
                }
                catch
                {
                    _trackedKeys.Clear();
                }
            }
        }

        private void SaveTrackedKeys()
        {
            string trackedKeysJson = _serializer.Serialize(_trackedKeys);
            PlayerPrefs.SetString($"{_prefix}_TrackedKeys", trackedKeysJson);
            PlayerPrefs.Save();
        }

        private void TrackKey(string key)
        {
            if (!_trackedKeys.Contains(key))
            {
                _trackedKeys.Add(key);
                SaveTrackedKeys();
            }
        }

        private void UntrackKey(string key)
        {
            _trackedKeys.Remove(key);
            SaveTrackedKeys();
        }

        public void Save<T>(string key, T data)
        {
            try
            {
                string fullKey = GetKey(key);
                string json = _serializer.Serialize(data);
                PlayerPrefs.SetString(fullKey, json);
                PlayerPrefs.Save();
                TrackKey(key);
            }
            catch (Exception ex)
            {
                Debug.LogError($"保存JSON到PlayerPrefs失败 [Key: {key}]: {ex.Message}");
            }
        }

        public T Load<T>(string key)
        {
            try
            {
                string fullKey = GetKey(key);
                
                if (!PlayerPrefs.HasKey(fullKey))
                {
                    return default;
                }

                string json = PlayerPrefs.GetString(fullKey);
                return _serializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"从PlayerPrefs加载JSON失败 [Key: {key}]: {ex.Message}");
                return default;
            }
        }

        public bool Exists(string key)
        {
            return PlayerPrefs.HasKey(GetKey(key));
        }

        public void Delete(string key)
        {
            try
            {
                string fullKey = GetKey(key);
                
                if (PlayerPrefs.HasKey(fullKey))
                {
                    PlayerPrefs.DeleteKey(fullKey);
                    PlayerPrefs.Save();
                    UntrackKey(key);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"从PlayerPrefs删除JSON失败 [Key: {key}]: {ex.Message}");
            }
        }

        public void Clear()
        {
            try
            {
                foreach (var key in _trackedKeys.ToList())
                {
                    PlayerPrefs.DeleteKey(GetKey(key));
                }
                
                _trackedKeys.Clear();
                SaveTrackedKeys();
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"清空PlayerPrefs JSON存储失败: {ex.Message}");
            }
        }

        public IEnumerable<string> GetAllKeys()
        {
            return _trackedKeys.ToList();
        }

        public async Task SaveAsync<T>(string key, T data)
        {
            await Task.Run(() => Save(key, data));
        }

        public async Task<T> LoadAsync<T>(string key)
        {
            return await Task.Run(() => Load<T>(key));
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await Task.Run(() => Exists(key));
        }

        public async Task DeleteAsync(string key)
        {
            await Task.Run(() => Delete(key));
        }

        public async Task ClearAsync()
        {
            await Task.Run(() => Clear());
        }
    }
}
