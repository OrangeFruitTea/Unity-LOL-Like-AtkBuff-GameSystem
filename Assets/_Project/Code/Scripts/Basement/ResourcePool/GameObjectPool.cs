using System;
using UnityEngine;

namespace Basement.ResourceManagement
{
    public class GameObjectPool : IResourcePool<GameObject>
    {
        private readonly SafeObjectPool<GameObject> _internalPool;
        private int _usedCount = 0;
        private readonly object _lock = new object();
        private readonly Transform _poolParent;

        public GameObjectPool(GameObject prefab, Transform poolParent = null, 
            int initialCapacity = 0, int maxCapacity = 100)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            _poolParent = poolParent ?? new GameObject("GameObjectPool").transform;
            _poolParent.gameObject.SetActive(false);

            // 创建游戏对象的函数
            Func<GameObject> createFunc = () =>
            {
                GameObject obj = GameObject.Instantiate(prefab, _poolParent);
                obj.name = prefab.name;
                obj.SetActive(false);
                return obj;
            };

            // 重置游戏对象的函数
            Action<GameObject> resetAction = (obj) =>
            {
                if (obj != null)
                {
                    obj.transform.SetParent(_poolParent);
                    obj.SetActive(false);
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;
                    obj.transform.localScale = Vector3.one;

                    // 调用IReusable接口
                    if (obj.TryGetComponent(out IReusable reusable))
                    {
                        reusable.OnDespawn();
                    }
                }
            };

            _internalPool = new SafeObjectPool<GameObject>(createFunc, resetAction, initialCapacity, maxCapacity);
        }

        public GameObject Spawn()
        {
            return Spawn(Vector3.zero, Quaternion.identity, null);
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            lock (_lock)
            {
                GameObject obj = _internalPool.get();
                
                if (obj != null)
                {
                    obj.transform.SetParent(parent, false);
                    obj.transform.position = position;
                    obj.transform.rotation = rotation;
                    obj.SetActive(true);
                    _usedCount++;

                    // 调用IReusable接口
                    if (obj.TryGetComponent(out IReusable reusable))
                    {
                        reusable.OnSpawn();
                    }
                }
                
                return obj;
            }
        }

        public bool TrySpawn(out GameObject obj)
        {
            return TrySpawn(out obj, Vector3.zero, Quaternion.identity, null);
        }

        public bool TrySpawn(out GameObject obj, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            lock (_lock)
            {
                if (_internalPool.TryGet(out obj))
                {
                    obj.transform.SetParent(parent, false);
                    obj.transform.position = position;
                    obj.transform.rotation = rotation;
                    obj.SetActive(true);
                    _usedCount++;

                    // 调用IReusable接口
                    if (obj.TryGetComponent(out IReusable reusable))
                    {
                        reusable.OnSpawn();
                    }
                    return true;
                }
                return false;
            }
        }

        public void Despawn(GameObject obj)
        {
            if (obj == null) return;

            lock (_lock)
            {
                _internalPool.Release(obj);
                _usedCount = Math.Max(0, _usedCount - 1);
            }
        }

        public void Preload(int count)
        {
            lock (_lock)
            {
                for (int i = 0; i < count; i++)
                {
                    GameObject obj = _internalPool.get();
                    _internalPool.Release(obj);
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _internalPool.Clear();
                _usedCount = 0;
            }
        }

        public int IdleCount
        {
            get { return _internalPool.IdleCount; }
        }

        public int UsedCount
        {
            get { lock (_lock) { return _usedCount; } }
        }
    }
}