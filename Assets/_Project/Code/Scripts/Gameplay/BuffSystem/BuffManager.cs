using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Entity;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    /// <summary>
    /// 固定时间更新的更新频率
    /// </summary>
    public const float FixedDeltaTime = 0.1f;

    public const int BuffCapacity = 25;

    #region Singleton
    private static BuffManager _instance;
    public static BuffManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject gameObjectInstance = new GameObject("Buff Manager");
                _instance = gameObjectInstance.AddComponent<BuffManager>();
                DontDestroyOnLoad(gameObjectInstance);
            }
            return _instance;
        }
    }
    #endregion

    /// <summary>
    /// 存储所有buff的字典，key为buff持有者, value为其所拥有的所有buff
    /// </summary>
    private readonly Dictionary<EntityBase, List<BuffBase>> _buffDictionary = new Dictionary<EntityBase, List<BuffBase>>(BuffCapacity);

    private readonly Dictionary<EntityBase, Action<BuffBase>> _observerDictionary =
        new Dictionary<EntityBase, Action<BuffBase>>(BuffCapacity);

    #region BuffPool

    private readonly Dictionary<Type, object> _buffPools = new Dictionary<Type, object>();
    private readonly object _poolLock = new object();

    private BuffPool<T> GetOrCreatePool<T>() where T : BuffBase, new()
    {
        var buffType = typeof(T);
        lock (_poolLock)
        {
            if (!_buffPools.ContainsKey(buffType))
            {
                var pool = new BuffPool<T>(
                    createFunc: () => new T(),
                    createFuncArgs: (args) =>
                    {
                        var buff = new T();
                        buff.Init(args);
                        return buff;
                    },
                    resetAction:buff=>buff.Reset(),
                    maxCapacity:15
                );
                _buffPools[buffType] = pool;
            }

            return (BuffPool<T>)_buffPools[buffType];
        }
    }
    

    #endregion

    #region PublicMethods

    /// <summary>
    /// 返回要观察的对象拥有的buff并在对象被添加新buff时发出通知
    /// 如果没有buff会返回空列表
    /// </summary>
    public List<BuffBase> StartObserving(EntityBase target, Action<BuffBase> listener)
    {
        List<BuffBase> list;
        // 添加监听
        if (!_observerDictionary.ContainsKey(target))
        {
            _observerDictionary.Add(target, null);
        }
        _observerDictionary[target] += listener;
        if (_buffDictionary.ContainsKey(target))
        {
            list = _buffDictionary[target];
        }
        else
        {
            list = new List<BuffBase>();
        }
        return list;
    }

    /// <summary>
    /// 停止观察某一对象，请传入与调用开始观察方法时使用的相同参数
    /// </summary>
    public void StopObserving(EntityBase target, Action<BuffBase> listener)
    {
        if (!_observerDictionary.ContainsKey(target))
        {
            throw new Exception("停止观察的对象目标不存在");
        }
        _observerDictionary[target] -= listener;
        if (_observerDictionary[target] == null)
        {
            _observerDictionary.Remove(target);
        }
    }

    public void AddBuff<T>(EntityBase target, EntityBase provider, uint level = 1, params object[] args) where T : BuffBase, new()
    {
        // 如果字典中没存储该key，则进行初始化
        if (!_buffDictionary.ContainsKey(target))
        {
            _buffDictionary.Add(target, new List<BuffBase>(BuffCapacity / 5));
            AddNewBuff<T>(target, provider, level, args);
            return;
        }

        if (_buffDictionary[target].Count == 0)
        {
            AddNewBuff<T>(target, provider, level, args);
            return;
        }
        // 遍历目标是否存在需要挂载的相同buff
        List<T> temp01 = new List<T>();
        foreach (BuffBase item in _buffDictionary[target])
        {
            if (item is T)
            {
                temp01.Add(item as T);
            }
        }
        // 如果存在相同buff则进行冲突处理
        if (temp01.Count == 0)
        {
            AddNewBuff<T>(target, provider, level, args);
        }
        else
        {
            switch (temp01[0].Config.resolution)
            {
                case BuffConflictResolution.Combine:
                    temp01[0].RuntimeData.CurrentLevel += level;
                    break;
                case BuffConflictResolution.Separate:
                    bool tmp = true;
                    foreach (T item in temp01)
                    {
                        if (item.RuntimeData.Provider == provider)
                        {
                            item.RuntimeData.CurrentLevel += level;
                            tmp = false;
                            break;
                        }
                    }
                    if (tmp)
                    {
                        AddNewBuff<T>(target, provider, level, args);
                    }
                    break;
                case BuffConflictResolution.Cover:
                    RemoveBuff(target, temp01[0]);
                    AddNewBuff<T>(target, provider, level, args);
                    break;
            }
        }
    }
    /// <summary>
    /// 获取目标身上指定类型的buff列表
    /// </summary>
    /// <param name="target"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public List<T> FindBuff<T>(EntityBase target) where T : BuffBase, new()
    {
        var res = new List<T>();
        if (_buffDictionary.TryGetValue(target, out var buff))
        {
            foreach (BuffBase item in buff)
            {
                if (item is T)
                {
                    res.Add(item as T);
                }
            }
        }

        return res;
    }
    /// <summary>
    /// 获得目标身上的所有buff
    /// 如果目标身上没有buff，返回空列表
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public List<BuffBase> FindAllBuff(EntityBase target)
    {
        List<BuffBase> res = new List<BuffBase>();
        if (_buffDictionary.TryGetValue(target, out var value))
        {
            res = value;
        }

        return res;
    }
    /// <summary>
    /// 移除目标身上指定的一个buff
    /// </summary>
    /// <param name="target"></param>
    /// <param name="buff"></param>
    /// <returns>是否成功，如果失败则说明目标不存在</returns>
    public bool RemoveBuff(EntityBase target, BuffBase buff)
    {
        if (!_buffDictionary.ContainsKey(target))
        {
            return false;
        }
        bool haveTarget = false;
        foreach (BuffBase item in _buffDictionary[target])
        {
            if (item == buff)
            {
                haveTarget = true;
                item.RuntimeData.CurrentLevel -= item.RuntimeData.CurrentLevel;
                item.OnLost();
                _buffDictionary[target].Remove(item);
                break;
            }
        }
        if (!haveTarget) return false;
        return true;
    }
    #endregion

    #region PrivateMethods
    private void AddNewBuff<T>(EntityBase target, EntityBase provider, uint level, params object[] args) where T : BuffBase, new()
    {
        var pool = GetOrCreatePool<T>();
        T buff;
        if (args == null || args.Length == 0) buff = pool.Get();
        else buff = pool.Get(args);
        _buffDictionary[target].Add(buff);
        buff.OnGet();
        if (_observerDictionary.ContainsKey(target))
        {
            _observerDictionary[target]?.Invoke(buff);
        }
    }

    #endregion
    
    // 协程部分
    private WaitForSeconds _waitForFixedDeltaTimeSeconds = new WaitForSeconds(FixedDeltaTime);
    private IEnumerator ExecuteFixedUpdate()
    {
        while (true)
        {
            yield return _waitForFixedDeltaTimeSeconds;
            // 执行所有buff的update
            foreach (var item1 in _buffDictionary)
            {
                foreach (BuffBase buff in item1.Value)
                {
                    if (buff.RuntimeData.CurrentLevel > 0 && buff.RuntimeData.Owner != null)
                    {
                        buff.FixedUpdate();
                    }
                }
            }
        }
    }

    private WaitForSeconds _waitFor10Seconds = new WaitForSeconds(10f);
    private Dictionary<EntityBase, List<BuffBase>> _buffDictionaryCopy = new Dictionary<EntityBase, List<BuffBase>>(25);

    private IEnumerator ExecuteGarbageCollection()
    {
        while (true)
        {
            yield return _waitFor10Seconds;
            foreach (var item in _buffDictionary)
            {
                _buffDictionaryCopy.Add(item.Key, item.Value);
            }
            // 清理无用的buff对象
            foreach (var item in _buffDictionaryCopy)
            {
                // 如果owner被删除，则让buff也同时删除
                if (item.Key.Equals(null))
                {
                    _buffDictionary.Remove(item.Key);
                    continue;
                }
                // 如果owner身上没有任何buff，也将其删除
                if (item.Value.Count == 0)
                {
                    _buffDictionary.Remove(item.Key);
                    continue;
                }
            }
        }
    }

    private void Awake()
    {
        StartCoroutine(ExecuteFixedUpdate());
        StartCoroutine(ExecuteGarbageCollection());
    }

    private BuffBase _transferBuff;

    private void FixedUpdate()
    {
        // 清理无用对象
        foreach (var item in _buffDictionaryCopy)
        {
            // 清理无用buff
            // 降低持续时间
            for (int i = item.Value.Count - 1; i >= 0; i--)
            {
                _transferBuff = item.Value[i];
                // 若等级为0,则将其移除
                if (_transferBuff.RuntimeData.CurrentLevel == 0)
                {
                    RemoveBuff(item.Key, _transferBuff);
                    continue;
                }
                // 如果持续时间为0, 则降级
                // 若降级后等级为0，则移除，否则刷新持续时间
                if (_transferBuff.RuntimeData.ResidualDuration == 0)
                {
                    _transferBuff.RuntimeData.CurrentLevel -= _transferBuff.Config.demotion;
                    if (_transferBuff.RuntimeData.CurrentLevel == 0)
                    {
                        RemoveBuff(item.Key, _transferBuff);
                        continue;
                    }
                    else
                    {
                        _transferBuff.RuntimeData.ResidualDuration = _transferBuff.Config.maxDuration;
                    }
                }
                // 降低持续时间
                _transferBuff.RuntimeData.ResidualDuration -= Time.fixedDeltaTime;
            } 
        }
    }
}
