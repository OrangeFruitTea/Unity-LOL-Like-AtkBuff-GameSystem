using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuffPool<T> where T : BuffBase, new()
{
    // 空闲待用的buff队列
    private readonly Queue<T> _idleBuffs = new Queue<T>();
    // 包括活跃和空闲的所有buff，用于查询
    private readonly HashSet<T> _allBuffs = new HashSet<T>();
    private readonly object _lock = new object();

    private readonly Func<T> _createFunc;
    // 多参数创建工厂
    private readonly Func<object[], T> _createFuncArgs;
    private readonly Action<T> _resetAction;
    private readonly int _maxCapacity;

    public BuffPool(Func<T> createFunc,Func<object[], T> createFuncArgs, Action<T> resetAction, int maxCapacity = 20)
    {
        _createFunc = createFunc ?? throw new ArgumentException(nameof(createFunc));
        _createFuncArgs = createFuncArgs ?? throw new ArgumentException(nameof(createFunc));
        _resetAction = resetAction;
        _maxCapacity = maxCapacity;
    }

    public T Get()
    {
        lock (_lock)
        {
            if (_idleBuffs.Count > 0)
            {
                var buff = _idleBuffs.Dequeue();
                return buff;
            }
        }
        var newBuff = _createFunc();
        lock (_lock)
        {
            _allBuffs.Add(newBuff); 
        }
        return newBuff;
    }

    public T Get(params object[] args)
    {
        lock (_lock)
        {
            if (_idleBuffs.Count > 0)
            {
                var buff = _idleBuffs.Dequeue();
                if (args != null && args.Length > 0)
                {
                    buff.Init(args);
                }

                return buff;
            }
        }

        var newBuff = _createFuncArgs(args);
        lock (_lock)
        {
            _allBuffs.Add(newBuff); 
        }

        return newBuff;
    }

    public void Release(T buff)
    {
        if (buff == null) return;
        _resetAction?.Invoke(buff);
        lock (_lock)
        {
            if (!_allBuffs.Contains(buff)) return;
            if (_idleBuffs.Count >= _maxCapacity)
            {
                _allBuffs.Remove(buff);
                return;
            }
            _idleBuffs.Enqueue(buff);
        }
    }

    public List<T> FindIdleBuffs(Predicate<T> predicate)
    {
        lock (_lock)
        {
            return _idleBuffs.Where(b => predicate(b)).ToList();
        }
    }

    public List<T> FindAllBuffs(Predicate<T> predicate)
    {
        lock (_lock)
        {
            return _allBuffs.Where(b => predicate(b)).ToList();
        }
    }
}