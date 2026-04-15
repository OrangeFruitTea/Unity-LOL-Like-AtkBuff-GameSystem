using System;
using System.Collections.Generic;

namespace Basement.ResourceManagement
{
    public class GenericResourcePool<T> : IResourcePool<T> where T : class
    {
        private readonly SafeObjectPool<T> _internalPool;
        private int _usedCount = 0;
        private readonly object _lock = new object();
        private readonly Action<T> _onSpawnAction;
        private readonly Action<T> _onDespawnAction;

        public GenericResourcePool(Func<T> createFunc, Action<T> onSpawnAction = null, Action<T> onDespawnAction = null, 
            int initialCapacity = 0, int maxCapacity = 100)
        {
            // 包装创建函数，确保实现了IReusable接口的对象能正确初始化
            Func<T> wrappedCreateFunc = () =>
            {
                T obj = createFunc();
                if (obj is IReusable reusable) {
                    reusable.OnSpawn();
                }
                return obj;
            };

            // 包装重置函数，实现IReusable接口的对象能正确回收
            Action<T> wrappedResetAction = (obj) =>
            {
                if (obj is IReusable reusable) {
                    reusable.OnDespawn();
                }
                onDespawnAction?.Invoke(obj);
            };

            _internalPool = new SafeObjectPool<T>(wrappedCreateFunc, wrappedResetAction, initialCapacity, maxCapacity);
            _onSpawnAction = onSpawnAction;
            _onDespawnAction = onDespawnAction;
        }

        public T Spawn()
        {
            lock (_lock)
            {
                T obj = _internalPool.get();
                _usedCount++;
                _onSpawnAction?.Invoke(obj);
                return obj;
            }
        }

        public bool TrySpawn(out T obj)
        {
            lock (_lock)
            {
                if (_internalPool.TryGet(out obj))
                {
                    _usedCount++;
                    _onSpawnAction?.Invoke(obj);
                    return true;
                }
                return false;
            }
        }

        public void Despawn(T obj)
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
                    T obj = _internalPool.get();
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