using System;
using System.Collections.Generic;
using UnityEngine;
using Basement.Utils;

namespace Basement.ResourceManagement
{
    public class ResourcePoolManager : Singleton<ResourcePoolManager>
    {
        private Dictionary<string, IResourcePool<GameObject>> _gameObjectPools = new Dictionary<string, IResourcePool<GameObject>>();
        private Dictionary<string, object> _genericPools = new Dictionary<string, object>();
        private IResourceLoader _resourceLoader;
        private readonly object _lock = new object();

        public void Initialize(IResourceLoader resourceLoader = null)
        {
            _resourceLoader = resourceLoader ?? new ResourceLoader();
        }

        // 游戏对象池相关方法
        public GameObjectPool CreateGameObjectPool(string poolKey, GameObject prefab, int initialCapacity = 10, int maxCapacity = 100)
        {
            lock (_lock)
            {
                if (_gameObjectPools.ContainsKey(poolKey))
                {
                    Debug.LogWarning($"GameObject pool with key '{poolKey}' already exists!");
                    return _gameObjectPools[poolKey] as GameObjectPool;
                }

                var pool = new GameObjectPool(prefab, null, initialCapacity, maxCapacity);
                _gameObjectPools[poolKey] = pool;
                return pool;
            }
        }

        public GameObjectPool CreateGameObjectPoolFromResource(string poolKey, string resourcePath, int initialCapacity = 10, int maxCapacity = 100)
        {
            var prefab = _resourceLoader.LoadSync<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load prefab from path: {resourcePath}");
                return null;
            }
            return CreateGameObjectPool(poolKey, prefab, initialCapacity, maxCapacity);
        }

        public GameObject SpawnGameObject(string poolKey)
        {
            return SpawnGameObject(poolKey, Vector3.zero, Quaternion.identity, null);
        }

        public GameObject SpawnGameObject(string poolKey, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            lock (_lock)
            {
                if (_gameObjectPools.TryGetValue(poolKey, out var pool))
                {
                    if (pool is GameObjectPool gameObjectPool)
                    {
                        return gameObjectPool.Spawn(position, rotation, parent);
                    }
                }
                Debug.LogError($"GameObject pool with key '{poolKey}' not found!");
                return null;
            }
        }

        public void DespawnGameObject(string poolKey, GameObject obj)
        {
            lock (_lock)
            {
                if (_gameObjectPools.TryGetValue(poolKey, out var pool))
                {
                    pool.Despawn(obj);
                }
                else
                {
                    Debug.LogError($"GameObject pool with key '{poolKey}' not found!");
                }
            }
        }

        // 通用资源池相关方法
        public GenericResourcePool<T> CreateGenericPool<T>(string poolKey, Func<T> createFunc, Action<T> onSpawnAction = null, Action<T> onDespawnAction = null, int initialCapacity = 10, int maxCapacity = 100) where T : class
        {
            lock (_lock)
            {
                string fullKey = $"{typeof(T).Name}_{poolKey}";
                if (_genericPools.ContainsKey(fullKey))
                {
                    Debug.LogWarning($"Generic pool with key '{fullKey}' already exists!");
                    return _genericPools[fullKey] as GenericResourcePool<T>;
                }

                var pool = new GenericResourcePool<T>(createFunc, onSpawnAction, onDespawnAction, initialCapacity, maxCapacity);
                _genericPools[fullKey] = pool;
                return pool;
            }
        }

        public T SpawnGeneric<T>(string poolKey) where T : class
        {
            lock (_lock)
            {
                string fullKey = $"{typeof(T).Name}_{poolKey}";
                if (_genericPools.TryGetValue(fullKey, out var pool))
                {
                    if (pool is GenericResourcePool<T> genericPool)
                    {
                        return genericPool.Spawn();
                    }
                }
                Debug.LogError($"Generic pool with key '{fullKey}' not found!");
                return null;
            }
        }

        public void DespawnGeneric<T>(string poolKey, T obj) where T : class
        {
            lock (_lock)
            {
                string fullKey = $"{typeof(T).Name}_{poolKey}";
                if (_genericPools.TryGetValue(fullKey, out var pool))
                {
                    if (pool is GenericResourcePool<T> genericPool)
                    {
                        genericPool.Despawn(obj);
                    }
                }
                else
                {
                    Debug.LogError($"Generic pool with key '{fullKey}' not found!");
                }
            }
        }

        // 预加载方法
        public void PreloadGameObjectPool(string poolKey, int count)
        {
            lock (_lock)
            {
                if (_gameObjectPools.TryGetValue(poolKey, out var pool))
                {
                    pool.Preload(count);
                }
                else
                {
                    Debug.LogError($"GameObject pool with key '{poolKey}' not found!");
                }
            }
        }

        public void PreloadGenericPool<T>(string poolKey, int count) where T : class
        {
            lock (_lock)
            {
                string fullKey = $"{typeof(T).Name}_{poolKey}";
                if (_genericPools.TryGetValue(fullKey, out var pool))
                {
                    if (pool is GenericResourcePool<T> genericPool)
                    {
                        genericPool.Preload(count);
                    }
                }
                else
                {
                    Debug.LogError($"Generic pool with key '{fullKey}' not found!");
                }
            }
        }

        // 清空所有资源池
        public void ClearAllPools()
        {
            lock (_lock)
            {
                foreach (var pool in _gameObjectPools.Values)
                {
                    pool.Clear();
                }
                _gameObjectPools.Clear();

                foreach (var pool in _genericPools.Values)
                {
                    if (pool is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _genericPools.Clear();
            }
        }

        // 获取资源加载器
        public IResourceLoader GetResourceLoader()
        {
            return _resourceLoader;
        }

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }
    }
}