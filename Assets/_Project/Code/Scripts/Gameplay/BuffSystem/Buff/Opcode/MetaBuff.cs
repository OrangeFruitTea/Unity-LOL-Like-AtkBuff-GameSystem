using System;
using UnityEngine;

/// <summary>
/// 表驱动 opcode 宿主：<see cref="OnGet"/> 执行 <see cref="BuffEffectComposition.OnApply"/>；可选按间隔执行 <c>OnPeriodicTick</c>（MVP 单层 §9.1）。<br/>
/// <c>BuffConfig.id</c> 仅为池化占位；真实 <c>buffConfigId</c> 由 <see cref="MetaBuffApplyFactory"/> 注入自定义参数。
/// </summary>
public sealed class MetaBuff : BuffBase
{
    private int _resolvedBuffConfigId;

    private BuffEffectComposition _composition;

    /// <summary> &lt;=0 则不执行周期 opcode。 </summary>
    private float _periodicIntervalSeconds = -1f;

    private double _lastPeriodicDispatchTime;

    private static BuffConfig BuildPoolPlaceholderConfig()
    {
        var c = new BuffConfig(BuffType.Debuff, BuffConflictResolution.Separate, maxDuration: 8f,
            frequency: 1f, maxLevel: 99, demotion: 1u, dispellable: true);
        c.id = 0;
        return c;
    }

    private static readonly BuffMetadata PlaceholderMeta = new BuffMetadata("OpcodeMeta", "MVP Opcode host", "None");

    public MetaBuff() : base(BuildPoolPlaceholderConfig(), PlaceholderMeta)
    {
    }

    protected override void HandleCustomArgs(object[] args)
    {
        if (args == null || args.Length < 1)
            throw new ArgumentException("MetaBuff 需要自定义参数：[buffConfigId:int]");

        _resolvedBuffConfigId = Convert.ToInt32(args[0]);
        if (!BuffOpcodeMvpDefinitions.TryGetComposition(_resolvedBuffConfigId, out _composition))
        {
            Debug.LogWarning($"[MetaBuff] 未注册 MVP composition：buffConfigId={_resolvedBuffConfigId}");
            _composition = new BuffEffectComposition();
        }

        _periodicIntervalSeconds = BuffOpcodeMvpDefinitions.GetPeriodicIntervalSeconds(_resolvedBuffConfigId);
        _lastPeriodicDispatchTime = -1;
    }

    public override void OnGet()
    {
        base.OnGet();
        if (!RuntimeData.IsInitialized || RuntimeData.Provider == null || RuntimeData.Owner == null)
            return;
        if (_composition == null)
            return;

        BuffOpcodeDispatcher.RunOnApplyInstructions(
            _composition.OnApply,
            RuntimeData.Provider,
            RuntimeData.Owner,
            RuntimeData);

        if (_periodicIntervalSeconds > 0f &&
            _composition.OnPeriodicTick != null &&
            _composition.OnPeriodicTick.Count > 0)
            _lastPeriodicDispatchTime = Time.timeAsDouble;
    }

    public override void FixedUpdate()
    {
        if (_composition?.OnPeriodicTick == null || _composition.OnPeriodicTick.Count == 0)
            return;
        if (_periodicIntervalSeconds <= 0f)
            return;
        if (!RuntimeData.IsInitialized || RuntimeData.Provider == null || RuntimeData.Owner == null)
            return;
        if (_lastPeriodicDispatchTime < 0)
            return;

        double now = Time.timeAsDouble;
        if (now - _lastPeriodicDispatchTime < _periodicIntervalSeconds)
            return;

        BuffOpcodeDispatcher.RunPeriodicInstructions(
            _composition.OnPeriodicTick,
            RuntimeData.Provider,
            RuntimeData.Owner,
            RuntimeData);

        _lastPeriodicDispatchTime = now;
    }
}
