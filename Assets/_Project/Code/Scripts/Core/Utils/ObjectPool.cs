using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 通用泛型对象池
public class SafeObjectPool<T> where T : class
{
    private readonly object _lock = new object();
    // 空闲对象队列（等待复用）
    private readonly Queue<T> _idleObjects = new Queue<T>();
    // 对象创建工厂（用于生成新对象）
    private readonly Func<T> _createFunc;
    // 对象重置回调（回收时清理状态）
    private readonly Action<T> _resetAction;
    // 最大缓存数量
    private readonly int _maxCapacity;
    // 空闲对象数量
    public int IdleCount
    {
        get
        {
            lock(_lock) { return _idleObjects.Count; }
        }
    }

    public SafeObjectPool(Func<T> createFunc, Action<T> resetAction = null, int initialCapacity = 0,
        int maxCapacity = 100)
    {
        _createFunc = createFunc ?? throw new ArgumentException(nameof(createFunc));
        _resetAction = resetAction;
        _maxCapacity = maxCapacity;
        if (initialCapacity > 0)
        {
            lock (_lock)
            {
                for (int i = 0; i < initialCapacity; i++)
                {
                    T obj = _createFunc();
                    _idleObjects.Enqueue(obj);
                }
            }
        }
    }

    public T get()
    {
        lock (_lock)
        {
            if (_idleObjects.Count > 0) return _idleObjects.Dequeue();
        }

        return _createFunc();
    }

    public void Release(T obj)
    {
        if (obj == null) throw new ArgumentException(nameof(obj));
        _resetAction?.Invoke(obj);
        lock (_lock)
        {
            if (_idleObjects.Count < _maxCapacity)
            {
                _idleObjects.Enqueue(obj);
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _idleObjects.Clear();
        }
    }

    public bool TryGet(out T obj)
    {
        lock (_lock)
        {
            if (_idleObjects.Count > 0)
            {
                obj = _idleObjects.Dequeue();
                return true;
            }

            obj = null;
            return false;
        }
    }
}