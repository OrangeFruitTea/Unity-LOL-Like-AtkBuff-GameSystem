using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Entity;
using UnityEngine;



public class BuffBase : IBuffHandler
{
    public readonly BuffMetadata Metadata;
    public readonly BuffConfig Config;
    public readonly BuffRuntimeData RuntimeData;
    // private uint _curLevel = 1;
    // private float _residualDuration = 3;
    // private bool _initialized;

    // 自动实现属性的getter
    public BuffBase(BuffConfig config, BuffMetadata metadata)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        RuntimeData = new BuffRuntimeData();
    }

    public string ProviderName => RuntimeData.Provider.EntityName; 
    public string OwnerName => RuntimeData.Owner.EntityName; 
    
    /// <summary>
    /// 当owner获得buff时触发
    /// 由buffManager在合适时调用
    /// </summary>
    public virtual void OnGet() {}
    /// <summary>
    /// 当owner失去Buff时触发
    /// 由buffManager在合适时调用
    /// </summary>
    public virtual void OnLost() {}
    /// <summary>
    /// Update, 由BuffManager每物理帧调用
    /// </summary>
    public virtual void FixedUpdate() {}
    /// <summary>
    /// 当buff等级改变时调用
    /// </summary>
    /// <param name="change">Buff等级的改变值</param>
    public virtual void OnLevelChange(uint change) {}

    public virtual void Initialize(EntityBase paramProvider, EntityBase paramOwner)
    {
        if (RuntimeData.IsInitialized)
        {
            throw new InvalidOperationException("无法对已初始化的Buff再次初始化");
        }

        if (paramOwner == null || paramProvider == null)
        {
            throw new ArgumentException("Buff的初始化参数不能为空");
        }

        RuntimeData.Provider = paramProvider;
        RuntimeData.Owner = paramOwner;
        RuntimeData.IsInitialized = true;
    }

    public void Init(object[] args)
    {
        // 验证参数有效性
        if (args == null || args.Length < 3)
        {
            throw new ArgumentException("Init参数不足，至少需要[provider, owner, level]");
        }
        // 验证参数
        if (args == null || args.Length < 3)
            throw new ArgumentException("Init需要至少3个参数：[provider, owner, level]");

        // 解析核心参数
        RuntimeData.Provider = args[0] as EntityBase ?? throw new ArgumentException("参数0必须是EntityBase");
        RuntimeData.Owner = args[1] as EntityBase ?? throw new ArgumentException("参数1必须是EntityBase");
        var newLevel = (uint)args[2];
        RuntimeData.CurrentLevel = newLevel;

        // 处理可选的持续时间参数
        if (args.Length >= 4 && args[3] is float duration)
        {
            RuntimeData.ResidualDuration = duration;
        }
        else
        {
            RuntimeData.ResidualDuration = Config.maxDuration; // 使用配置的默认持续时间
        }

        // 处理其他自定义参数（留给子类实现）
        HandleCustomArgs(args.Skip(4).ToArray());
        RuntimeData.IsInitialized = true;
    }

    protected virtual void HandleCustomArgs(object[] args)
    {
    }

    public virtual void Reset()
    {
        RuntimeData.Clear();
    }

    public void HandleBuff(BuffBase buff)
    {
        throw new NotImplementedException();
    }
}
