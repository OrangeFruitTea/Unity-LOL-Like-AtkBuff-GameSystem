# 资源管理模块技术文档

## 概述

资源管理模块是基础设施层的核心组件，负责游戏资源的加载、卸载、缓存和生命周期管理。该模块基于资源池技术设计，实现了高效的资源复用机制，减少内存分配和GC压力，同时支持多种加载策略（同步/异步），满足不同场景的需求。

## 模块架构设计

### 1. 设计目标

- **资源复用**：通过对象池技术实现资源的复用，减少内存分配和GC压力
- **多加载策略**：支持同步和异步资源加载，适应不同场景需求
- **生命周期管理**：自动管理资源的加载、使用、卸载生命周期
- **性能优化**：减少资源加载时间和内存占用，提升游戏性能
- **易于扩展**：支持自定义资源类型和加载策略

### 2. 架构分层

```
┌─────────────────────────────────────────────────────────────┐
│                    业务逻辑层                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ 角色系统    │  │ 技能系统    │  │ 特效系统    │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    资源管理层                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ 资源池管理器 │  │ 资源加载器  │  │ 资源缓存    │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    资源实现层                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ GameObject  │  │ AudioClip   │  │ Texture2D   │  │
│  │ Sprite      │  │ Material    │  │ Prefab      │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
                    ┌─────────────┐
                    │Unity Engine │
                    │Resources/   │
                    │Addressables │
                    └─────────────┘
```

### 3. 核心组件

#### 3.1 可复用对象接口

```csharp
namespace Basement.ResourceManagement
{
    /// <summary>
    /// 可复用对象接口
    /// 实现此接口的对象可以参与资源池的自动管理
    /// </summary>
    public interface IReusable
    {
        /// <summary>
        /// 当对象被从池中取出时调用
        /// 用于初始化对象状态
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// 当对象被回收回池中时调用
        /// 用于重置对象状态
        /// </summary>
        void OnDespawn();
    }
}
```

#### 3.2 资源池接口

```csharp
namespace Basement.ResourceManagement
{
    /// <summary>
    /// 资源池接口
    /// 定义资源池的标准操作
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    public interface IResourcePool<T> where T : class
    {
        /// <summary>
        /// 从池中获取对象
        /// </summary>
        /// <returns>池中的对象</returns>
        T Spawn();

        /// <summary>
        /// 尝试从池中获取对象
        /// </summary>
        /// <param name="obj">输出的对象</param>
        /// <returns>是否成功获取</returns>
        bool TrySpawn(out T obj);

        /// <summary>
        /// 将对象回收回池中
        /// </summary>
        /// <param name="obj">要回收的对象</param>
        void Despawn(T obj);

        /// <summary>
        /// 预加载指定数量的对象
        /// </summary>
        /// <param name="count">预加载数量</param>
        void Preload(int count);

        /// <summary>
        /// 清空资源池
        /// </summary>
        void Clear();

        /// <summary>
        /// 获取当前空闲对象数量
        /// </summary>
        int IdleCount { get; }

        /// <summary>
        /// 获取当前已使用对象数量
        /// </summary>
        int UsedCount { get; }

        /// <summary>
        /// 获取池的最大容量
        /// </summary>
        int MaxCapacity { get; }
    }
}
```

#### 3.3 通用资源池实现

```csharp
using System;
using System.Collections.Generic;
using Basement.Utils;

namespace Basement.ResourceManagement
{
    /// <summary>
    /// 通用资源池实现
    /// 支持任意引用类型的对象池管理
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    public class GenericResourcePool<T> : IResourcePool<T> where T : class
    {
        private readonly Stack<T> _pool;
        private readonly Func<T> _createFunc;
        private readonly Action<T> _resetAction;
        private readonly int _maxCapacity;
        private int _usedCount;
        private readonly object _lock = new object();

        public GenericResourcePool(
            Func<T> createFunc,
            Action<T> resetAction = null,
            int initialCapacity = 0,
            int maxCapacity = 100)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _resetAction = resetAction;
            _maxCapacity = maxCapacity;
            _pool = new Stack<T>(initialCapacity);

            Preload(initialCapacity);
        }

        public T Spawn()
        {
            lock (_lock)
            {
                T obj = _pool.Count > 0 ? _pool.Pop() : _createFunc();

                if (obj is IReusable reusable)
                {
                    reusable.OnSpawn();
                }

                _usedCount++;
                return obj;
            }
        }

        public bool TrySpawn(out T obj)
        {
            lock (_lock)
            {
                if (_pool.Count > 0 || _usedCount < _maxCapacity)
                {
                    obj = Spawn();
                    return true;
                }

                obj = null;
                return false;
            }
        }

        public void Despawn(T obj)
        {
            if (obj == null) return;

            lock (_lock)
            {
                if (_pool.Count < _maxCapacity)
                {
                    if (obj is IReusable reusable)
                    {
                        reusable.OnDespawn();
                    }

                    _resetAction?.Invoke(obj);
                    _pool.Push(obj);
                    _usedCount--;
                }
                else
                {
                    // 超过容量限制，直接销毁
                    if (obj is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    _usedCount--;
                }
            }
        }

        public void Preload(int count)
        {
            lock (_lock)
            {
                int loadCount = Math.Min(count, _maxCapacity - _pool.Count - _usedCount);
                for (int i = 0; i < loadCount; i++)
                {
                    T obj = _createFunc();
                    if (obj is IReusable reusable)
                    {
                        reusable.OnDespawn();
                    }
                    _pool.Push(obj);
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                while (_pool.Count > 0)
                {
                    T obj = _pool.Pop();
                    if (obj is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _usedCount = 0;
            }
        }

        public int IdleCount
        {
            get
            {
                lock (_lock)
                {
                    return _pool.Count;
                }
            }
        }

        public int UsedCount
        {
            get
            {
                lock (_lock)
                {
                    return _usedCount;
                }
            }
        }

        public int MaxCapacity => _maxCapacity;
    }
}
```

#### 3.4 GameObject资源池

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Basement.ResourceManagement
{
    /// <summary>
    /// GameObject资源池
    /// 专门用于Unity GameObject的池化管理
    /// </summary>
    public class GameObjectPool : IResourcePool<GameObject>
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Stack<GameObject> _pool;
        private readonly int _maxCapacity;
        private int _usedCount;
        private readonly object _lock = new object();

        public GameObjectPool(
            GameObject prefab,
            Transform parent = null,
            int initialCapacity = 0,
            int maxCapacity = 100)
        {
            _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            _parent = parent;
            _maxCapacity = maxCapacity;
            _pool = new Stack<GameObject>(initialCapacity);

            Preload(initialCapacity);
        }

        public GameObject Spawn()
        {
            lock (_lock)
            {
                GameObject obj;

                if (_pool.Count > 0)
                {
                    obj = _pool.Pop();
                    obj.SetActive(true);
                }
                else
                {
                    obj = UnityEngine.Object.Instantiate(_prefab, _parent);
                }

                if (obj.TryGetComponent<IReusable>(out var reusable))
                {
                    reusable.OnSpawn();
                }

                _usedCount++;
                return obj;
            }
        }

        public bool TrySpawn(out GameObject obj)
        {
            lock (_lock)
            {
                if (_pool.Count > 0 || _usedCount < _maxCapacity)
                {
                    obj = Spawn();
                    return true;
                }

                obj = null;
                return false;
            }
        }

        public void Despawn(GameObject obj)
        {
            if (obj == null) return;

            lock (_lock)
            {
                if (_pool.Count < _maxCapacity)
                {
                    if (obj.TryGetComponent<IReusable>(out var reusable))
                    {
                        reusable.OnDespawn();
                    }

                    obj.SetActive(false);
                    obj.transform.SetParent(_parent);
                    _pool.Push(obj);
                    _usedCount--;
                }
                else
                {
                    UnityEngine.Object.Destroy(obj);
                    _usedCount--;
                }
            }
        }

        public void Preload(int count)
        {
            lock (_lock)
            {
                int loadCount = Math.Min(count, _maxCapacity - _pool.Count - _usedCount);
                for (int i = 0; i < loadCount; i++)
                {
                    GameObject obj = UnityEngine.Object.Instantiate(_prefab, _parent);
                    obj.SetActive(false);

                    if (obj.TryGetComponent<IReusable>(out var reusable))
                    {
                        reusable.OnDespawn();
                    }

                    _pool.Push(obj);
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                while (_pool.Count > 0)
                {
                    GameObject obj = _pool.Pop();
                    UnityEngine.Object.Destroy(obj);
                }
                _usedCount = 0;
            }
        }

        public int IdleCount
        {
            get
            {
                lock (_lock)
                {
                    return _pool.Count;
                }
            }
        }

        public int UsedCount
        {
            get
            {
                lock (_lock)
                {
                    return _usedCount;
                }
            }
        }

        public int MaxCapacity => _maxCapacity;
    }
}
```

#### 3.5 资源加载器接口

```csharp
using System;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Basement.ResourceManagement
{
    /// <summary>
    /// 资源加载器接口
    /// 定义资源加载的标准操作
    /// </summary>
    public interface IResourceLoader
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="resourcePath">资源路径</param>
        /// <returns>加载的资源</returns>
        T LoadSync<T>(string resourcePath) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="resourcePath">资源路径</param>
        /// <param name="onLoaded">加载完成回调</param>
        /// <param name="onProgress">加载进度回调</param>
        void LoadAsync<T>(string resourcePath, Action<T> onLoaded, Action<AsyncOperationHandle<T>> onProgress = null) where T : UnityEngine.Object;

        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="resource">要卸载的资源</param>
        void Unload(UnityEngine.Object resource);

        /// <summary>
        /// 预加载资源
        /// </summary>
        /// <param name="resourcePaths">资源路径列表</param>
        /// <param name="onCompleted">完成回调</param>
        void Preload(string[] resourcePaths, Action onCompleted = null);
    }
}
```

#### 3.6 Unity资源加载器实现

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.AddressableAssets;

namespace Basement.ResourceManagement
{
    /// <summary>
    /// Unity资源加载器实现
    /// 支持Resources和Addressables两种加载方式
    /// </summary>
    public class UnityResourceLoader : IResourceLoader
    {
        private readonly Dictionary<string, AsyncOperationHandle> _loadedAssets = new Dictionary<string, AsyncOperationHandle>();
        private readonly object _lock = new object();

        public T LoadSync<T>(string resourcePath) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            // 编辑器模式下使用Resources加载，方便开发
            return Resources.Load<T>(resourcePath);
#else
            // 发布模式下使用Addressables加载
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(resourcePath);
            handle.WaitForCompletion();
            return handle.Result;
#endif
        }

        public void LoadAsync<T>(string resourcePath, Action<T> onLoaded, Action<AsyncOperationHandle<T>> onProgress = null) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            // 编辑器模式下使用Resources加载
            AsyncOperation asyncOp = Resources.LoadAsync<T>(resourcePath);
            asyncOp.completed += (op) =>
            {
                onLoaded?.Invoke(op.asset as T);
            };
#else
            // 发布模式下使用Addressables加载
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(resourcePath);

            if (onProgress != null)
            {
                handle.Completed += (h) =>
                {
                    onProgress?.Invoke(h);
                    onLoaded?.Invoke(h.Result);
                };
            }
            else
            {
                handle.Completed += (h) =>
                {
                    onLoaded?.Invoke(h.Result);
                };
            }

            lock (_lock)
            {
                if (!_loadedAssets.ContainsKey(resourcePath))
                {
                    _loadedAssets[resourcePath] = handle;
                }
            }
#endif
        }

        public void Unload(UnityEngine.Object resource)
        {
            if (resource == null) return;

#if UNITY_EDITOR
            Resources.UnloadAsset(resource);
#else
            // Addressables会自动管理引用计数
            // 这里不需要手动卸载
#endif
        }

        public void Preload(string[] resourcePaths, Action onCompleted = null)
        {
            if (resourcePaths == null || resourcePaths.Length == 0)
            {
                onCompleted?.Invoke();
                return;
            }

            MonoBehaviourHelper.Instance.StartCoroutine(PreloadCoroutine(resourcePaths, onCompleted));
        }

        private IEnumerator PreloadCoroutine(string[] resourcePaths, Action onCompleted)
        {
            int loadedCount = 0;
            int totalCount = resourcePaths.Length;

            foreach (string path in resourcePaths)
            {
                LoadAsync<UnityEngine.Object>(path, (asset) =>
                {
                    loadedCount++;
                });
            }

            while (loadedCount < totalCount)
            {
                yield return null;
            }

            onCompleted?.Invoke();
        }

        /// <summary>
        /// 释放所有已加载的资源
        /// </summary>
        public void ReleaseAll()
        {
            lock (_lock)
            {
                foreach (var kvp in _loadedAssets)
                {
                    Addressables.Release(kvp.Value);
                }
                _loadedAssets.Clear();
            }
        }
    }
}
```

#### 3.7 资源池管理器

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Basement.Utils;

namespace Basement.ResourceManagement
{
    /// <summary>
    /// 资源池管理器
    /// 负责管理所有资源池的创建、获取和销毁
    /// </summary>
    public class ResourcePoolManager : Singleton<ResourcePoolManager>
    {
        private readonly Dictionary<string, IResourcePool<GameObject>> _gameObjectPools = new Dictionary<string, IResourcePool<GameObject>>();
        private readonly Dictionary<Type, object> _genericPools = new Dictionary<Type, object>();
        private IResourceLoader _resourceLoader;

        protected override void Awake()
        {
            base.Awake();
            _resourceLoader = new UnityResourceLoader();
        }

        /// <summary>
        /// 获取或创建GameObject资源池
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="parent">父对象</param>
        /// <param name="initialCapacity">初始容量</param>
        /// <param name="maxCapacity">最大容量</param>
        /// <returns>资源池</returns>
        public IResourcePool<GameObject> GetGameObjectPool(
            GameObject prefab,
            Transform parent = null,
            int initialCapacity = 0,
            int maxCapacity = 100)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));

            string poolKey = prefab.name;

            if (!_gameObjectPools.TryGetValue(poolKey, out var pool))
            {
                pool = new GameObjectPool(prefab, parent, initialCapacity, maxCapacity);
                _gameObjectPools[poolKey] = pool;
            }

            return pool;
        }

        /// <summary>
        /// 获取或创建通用资源池
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="createFunc">创建函数</param>
        /// <param name="resetAction">重置函数</param>
        /// <param name="initialCapacity">初始容量</param>
        /// <param name="maxCapacity">最大容量</param>
        /// <returns>资源池</returns>
        public IResourcePool<T> GetGenericPool<T>(
            Func<T> createFunc,
            Action<T> resetAction = null,
            int initialCapacity = 0,
            int maxCapacity = 100) where T : class
        {
            Type poolType = typeof(T);

            if (!_genericPools.TryGetValue(poolType, out var poolObj))
            {
                var pool = new GenericResourcePool<T>(createFunc, resetAction, initialCapacity, maxCapacity);
                _genericPools[poolType] = pool;
                return pool;
            }

            return (IResourcePool<T>)poolObj;
        }

        /// <summary>
        /// 从资源池生成GameObject
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="parent">父对象</param>
        /// <returns>生成的GameObject</returns>
        public GameObject Spawn(GameObject prefab, Transform parent = null)
        {
            var pool = GetGameObjectPool(prefab, parent);
            return pool.Spawn();
        }

        /// <summary>
        /// 回收GameObject到资源池
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="obj">要回收的GameObject</param>
        public void Despawn(GameObject prefab, GameObject obj)
        {
            if (prefab == null || obj == null) return;

            string poolKey = prefab.name;

            if (_gameObjectPools.TryGetValue(poolKey, out var pool))
            {
                pool.Despawn(obj);
            }
            else
            {
                UnityEngine.Object.Destroy(obj);
            }
        }

        /// <summary>
        /// 预加载资源
        /// </summary>
        /// <param name="resourcePaths">资源路径列表</param>
        /// <param name="onCompleted">完成回调</param>
        public void PreloadResources(string[] resourcePaths, Action onCompleted = null)
        {
            _resourceLoader.Preload(resourcePaths, onCompleted);
        }

        /// <summary>
        /// 清空所有资源池
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _gameObjectPools.Values)
            {
                pool.Clear();
            }
            _gameObjectPools.Clear();

            foreach (var poolObj in _genericPools.Values)
            {
                if (poolObj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _genericPools.Clear();

            if (_resourceLoader is UnityResourceLoader unityLoader)
            {
                unityLoader.ReleaseAll();
            }
        }

        /// <summary>
        /// 获取资源加载器
        /// </summary>
        public IResourceLoader ResourceLoader => _resourceLoader;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ClearAllPools();
        }
    }
}
```

## 使用说明

### 1. GameObject资源池使用

```csharp
using UnityEngine;
using Basement.ResourceManagement;

public class BulletSpawner : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int initialPoolSize = 10;

    private IResourcePool<GameObject> _bulletPool;

    private void Start()
    {
        // 获取或创建子弹资源池
        _bulletPool = ResourcePoolManager.Instance.GetGameObjectPool(
            bulletPrefab,
            transform,
            initialPoolSize,
            50
        );

        // 预加载子弹
        _bulletPool.Preload(initialPoolSize);
    }

    public void FireBullet(Vector3 position, Vector3 direction)
    {
        // 从池中获取子弹
        GameObject bullet = _bulletPool.Spawn();

        bullet.transform.position = position;
        bullet.transform.rotation = Quaternion.LookRotation(direction);

        // 设置子弹逻辑
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        bulletComponent.Initialize(direction, OnBulletHit);
    }

    private void OnBulletHit(GameObject bullet)
    {
        // 回收子弹到池中
        _bulletPool.Despawn(bullet);
    }

    private void OnDestroy()
    {
        // 清空资源池
        _bulletPool?.Clear();
    }
}
```

### 2. 通用资源池使用

```csharp
using System.Collections.Generic;
using Basement.ResourceManagement;

public class EffectManager
{
    private IResourcePool<ParticleEffect> _effectPool;

    public EffectManager()
    {
        // 创建特效资源池
        _effectPool = ResourcePoolManager.Instance.GetGenericPool(
            createFunc: () => new ParticleEffect(),
            resetAction: (effect) => effect.Reset(),
            initialCapacity: 20,
            maxCapacity: 100
        );
    }

    public void PlayEffect(string effectName, Vector3 position)
    {
        // 从池中获取特效
        ParticleEffect effect = _effectPool.Spawn();
        effect.Play(effectName, position, OnEffectComplete);
    }

    private void OnEffectComplete(ParticleEffect effect)
    {
        // 回收特效到池中
        _effectPool.Despawn(effect);
    }
}
```

### 3. 资源加载使用

```csharp
using UnityEngine;
using Basement.ResourceManagement;

public class AssetLoader : MonoBehaviour
{
    private IResourceLoader _resourceLoader;

    private void Start()
    {
        _resourceLoader = ResourcePoolManager.Instance.ResourceLoader;

        // 同步加载资源
        Texture2D texture = _resourceLoader.LoadSync<Texture2D>("Textures/Player");

        // 异步加载资源
        _resourceLoader.LoadAsync<AudioClip>("Audio/Music/BGM", (clip) =>
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.Play();
        });

        // 预加载资源
        string[] preloadPaths = new string[]
        {
            "Textures/Player",
            "Audio/Music/BGM",
            "Prefabs/Enemy"
        };
        _resourceLoader.Preload(preloadPaths, () =>
        {
            Debug.Log("资源预加载完成");
        });
    }
}
```

### 4. 实现IReusable接口

```csharp
using UnityEngine;
using Basement.ResourceManagement;

public class Bullet : MonoBehaviour, IReusable
{
    private Vector3 _direction;
    private float _speed;
    private System.Action<GameObject> _onHitCallback;

    public void Initialize(Vector3 direction, System.Action<GameObject> onHitCallback)
    {
        _direction = direction;
        _speed = 10f;
        _onHitCallback = onHitCallback;
    }

    public void OnSpawn()
    {
        // 初始化子弹状态
        gameObject.SetActive(true);
    }

    public void OnDespawn()
    {
        // 重置子弹状态
        _direction = Vector3.zero;
        _speed = 0f;
        _onHitCallback = null;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;

        // 检测碰撞
        if (CheckCollision())
        {
            _onHitCallback?.Invoke(gameObject);
        }
    }

    private bool CheckCollision()
    {
        // 碰撞检测逻辑
        return false;
    }
}
```

## 性能优化策略

### 1. 动态扩容与收缩

```csharp
public class DynamicResourcePool<T> : IResourcePool<T> where T : class
{
    private int _currentCapacity;
    private float _expandThreshold = 0.8f;
    private float _shrinkThreshold = 0.2f;

    public T Spawn()
    {
        lock (_lock)
        {
            // 检查是否需要扩容
            if (_usedCount >= _currentCapacity * _expandThreshold)
            {
                ExpandPool();
            }

            T obj = _pool.Count > 0 ? _pool.Pop() : _createFunc();
            _usedCount++;
            return obj;
        }
    }

    public void Despawn(T obj)
    {
        lock (_lock)
        {
            _usedCount--;

            // 检查是否需要收缩
            if (_usedCount <= _currentCapacity * _shrinkThreshold && _currentCapacity > _initialCapacity)
            {
                ShrinkPool();
            }

            if (_pool.Count < _maxCapacity)
            {
                _resetAction?.Invoke(obj);
                _pool.Push(obj);
            }
            else
            {
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    private void ExpandPool()
    {
        int expandSize = Mathf.CeilToInt(_currentCapacity * 0.5f);
        int newCapacity = Mathf.Min(_currentCapacity + expandSize, _maxCapacity);

        for (int i = 0; i < newCapacity - _currentCapacity; i++)
        {
            T obj = _createFunc();
            _resetAction?.Invoke(obj);
            _pool.Push(obj);
        }

        _currentCapacity = newCapacity;
    }

    private void ShrinkPool()
    {
        int shrinkSize = Mathf.CeilToInt(_currentCapacity * 0.2f);
        int newCapacity = Mathf.Max(_currentCapacity - shrinkSize, _initialCapacity);

        while (_pool.Count > newCapacity)
        {
            T obj = _pool.Pop();
            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _currentCapacity = newCapacity;
    }
}
```

### 2. 分层资源池

```csharp
public class LayeredResourcePool<T> : IResourcePool<T> where T : class
{
    private readonly Dictionary<int, IResourcePool<T>> _layerPools = new Dictionary<int, IResourcePool<T>>();

    public IResourcePool<T> GetLayer(int layer)
    {
        if (!_layerPools.TryGetValue(layer, out var pool))
        {
            pool = new GenericResourcePool<T>(_createFunc, _resetAction, 0, _maxCapacity);
            _layerPools[layer] = pool;
        }
        return pool;
    }

    public T Spawn(int layer = 0)
    {
        return GetLayer(layer).Spawn();
    }

    public void Despawn(T obj, int layer = 0)
    {
        GetLayer(layer).Despawn(obj);
    }
}
```

### 3. 异步预加载

```csharp
public class AsyncPreloader
{
    public static IEnumerator PreloadPoolAsync<T>(IResourcePool<T> pool, int count, int batchSize = 5) where T : class
    {
        int loaded = 0;
        while (loaded < count)
        {
            int batch = Mathf.Min(batchSize, count - loaded);
            pool.Preload(batch);
            loaded += batch;
            yield return null;
        }
    }
}
```

### 4. 资源引用计数

```csharp
public class ReferenceCountedPool<T> : IResourcePool<T> where T : class
{
    private readonly Dictionary<T, int> _referenceCounts = new Dictionary<T, int>();

    public T Spawn()
    {
        T obj = _basePool.Spawn();
        lock (_referenceCounts)
        {
            _referenceCounts[obj] = 1;
        }
        return obj;
    }

    public void AddReference(T obj)
    {
        lock (_referenceCounts)
        {
            if (_referenceCounts.ContainsKey(obj))
            {
                _referenceCounts[obj]++;
            }
        }
    }

    public void ReleaseReference(T obj)
    {
        lock (_referenceCounts)
        {
            if (_referenceCounts.TryGetValue(obj, out int count))
            {
                count--;
                if (count <= 0)
                {
                    _referenceCounts.Remove(obj);
                    _basePool.Despawn(obj);
                }
                else
                {
                    _referenceCounts[obj] = count;
                }
            }
        }
    }
}
```

## 与Unity引擎的结合点

### 1. Addressables集成

```csharp
#if !UNITY_EDITOR
public class AddressablesResourceLoader : IResourceLoader
{
    public void LoadAsync<T>(string address, Action<T> onLoaded) where T : UnityEngine.Object
    {
        Addressables.LoadAssetAsync<T>(address).Completed += (handle) =>
        {
            onLoaded?.Invoke(handle.Result);
        };
    }

    public void Unload(UnityEngine.Object asset)
    {
        Addressables.Release(asset);
    }
}
#endif
```

### 2. 编辑器工具

```csharp
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(ResourcePoolManager))]
public class ResourcePoolManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("显示资源池统计"))
        {
            ShowPoolStatistics();
        }

        if (GUILayout.Button("清空所有资源池"))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要清空所有资源池吗？", "确定", "取消"))
            {
                ResourcePoolManager.Instance.ClearAllPools();
            }
        }
    }

    private void ShowPoolStatistics()
    {
        var manager = (ResourcePoolManager)target;
        // 显示资源池统计信息
    }
}
#endif
```

### 3. 资源监控

```csharp
public class ResourceMonitor : MonoBehaviour
{
    [SerializeField] private float updateInterval = 1f;

    private void Start()
    {
        StartCoroutine(MonitorCoroutine());
    }

    private IEnumerator MonitorCoroutine()
    {
        while (true)
        {
            LogPoolStatistics();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void LogPoolStatistics()
    {
        var manager = ResourcePoolManager.Instance;
        // 记录资源池统计信息
    }
}
```

## 总结

资源管理模块通过对象池技术和多种加载策略，实现了高效的资源管理，具有以下优势：

1. **性能优化**：减少内存分配和GC压力，提升游戏性能
2. **灵活加载**：支持同步和异步加载，适应不同场景需求
3. **易于使用**：提供简洁的API，降低开发复杂度
4. **可扩展性**：支持自定义资源类型和加载策略
5. **生命周期管理**：自动管理资源的加载、使用、卸载

通过使用资源管理模块，项目可以显著提升资源管理效率，降低内存占用，提高游戏性能。
