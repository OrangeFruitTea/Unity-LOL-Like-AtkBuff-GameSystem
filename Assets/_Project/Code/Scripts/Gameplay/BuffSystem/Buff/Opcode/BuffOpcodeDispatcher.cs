using System;
using Core.Combat;
using Core.ECS;
using Core.Gameplay;
using Core.Entity;
using UnityEngine;

/// <summary>
/// Opcode → Impact / Motor / CC / 操作锁 等 Dispatcher。<br/>
/// 完整实现按计划迭代；非 <see cref="BuffEffectOpcode.ImpactDamage"/> 路径当前为占位（编辑器下可加日志）。
/// </summary>
public static class BuffOpcodeDispatcher
{
    /// <summary> 在执行伤害类 opcode 时对最终数值乘以叠层（§4.3：<see cref="BuffRuntimeData.CurrentLevel"/>）。 </summary>
    public static void RunOnApplyInstructions(
        System.Collections.Generic.IReadOnlyList<BuffOpcodeInstruction> instructions,
        EntityBase provider,
        EntityBase owner,
        BuffRuntimeData runtimeData)
    {
        RunInstructions(instructions, provider, owner, runtimeData);
    }

    public static void RunPeriodicInstructions(
        System.Collections.Generic.IReadOnlyList<BuffOpcodeInstruction> instructions,
        EntityBase provider,
        EntityBase owner,
        BuffRuntimeData runtimeData)
    {
        RunInstructions(instructions, provider, owner, runtimeData);
    }

    private static void RunInstructions(
        System.Collections.Generic.IReadOnlyList<BuffOpcodeInstruction> instructions,
        EntityBase provider,
        EntityBase owner,
        BuffRuntimeData runtimeData)
    {
        if (instructions == null || instructions.Count == 0)
            return;
        if (provider == null || owner == null)
        {
            Debug.LogWarning("[BuffOpcodeDispatcher] 缺少 Provider / Owner。");
            return;
        }

        var world = EcsWorld.Instance;
        if (world == null)
        {
            Debug.LogWarning("[BuffOpcodeDispatcher] EcsWorld 未就绪。");
            return;
        }

        var sourcesEcs = provider.BoundEcsEntity;
        var targetEcs = owner.BoundEcsEntity;
        if (!sourcesEcs.IsValid() || !targetEcs.IsValid())
        {
            Debug.LogWarning("[BuffOpcodeDispatcher] BoundEcsEntity 无效。");
            return;
        }

        uint stacks = runtimeData != null ? System.Math.Max(1u, runtimeData.CurrentLevel) : 1u;
        var impacts = new ImpactManager(world);

        foreach (var ins in instructions)
        {
            switch (ins.Opcode)
            {
                case BuffEffectOpcode.ImpactDamage:
                    DispatchImpactDamage(impacts, ins, sourcesEcs, targetEcs, stacks);
                    break;

                case BuffEffectOpcode.ForcedDisplacement:
                    PlaceholderForcedDisplacement(ins, sourcesEcs, targetEcs, provider, owner);
                    break;

                case BuffEffectOpcode.ForcedPathfind:
                    PlaceholderForcedPathfind(ins, sourcesEcs, targetEcs, provider, owner);
                    break;

                case BuffEffectOpcode.CharacterOperationLock:
                    PlaceholderCharacterOperationLock(ins, targetEcs, owner);
                    break;

                case BuffEffectOpcode.ControlLock:
                    PlaceholderCrowdControlLock(ins, targetEcs, owner);
                    break;

                case BuffEffectOpcode.StatModifyCore:
                case BuffEffectOpcode.StatModifyBonus:
                    PlaceholderStatModify(ins, targetEcs, owner);
                    break;

                case BuffEffectOpcode.ApplyChildBuffById:
                    PlaceholderApplyChildBuff(ins, provider, owner);
                    break;

                case BuffEffectOpcode.None:
                    break;

                default:
                    LogOpcodeStub(ins.Opcode.ToString(), "未识别的枚举分支。");
                    break;
            }
        }
    }

    private static void DispatchImpactDamage(
        ImpactManager impacts,
        BuffOpcodeInstruction ins,
        EcsEntity sourcesEcs,
        EcsEntity targetEcs,
        uint stacks)
    {
        float baseVal = ins.ArgF0 * stacks;
        var impactType = (ImpactType)ins.ArgI0;
        if (!Enum.IsDefined(typeof(ImpactType), impactType))
            impactType = ImpactType.Magical;
        var sourceType = (ImpactSourceType)ins.ArgI1;
        if (!Enum.IsDefined(typeof(ImpactSourceType), sourceType))
            sourceType = ImpactSourceType.Skill;

        impacts.CreateImpactEvent(
            sourcesEcs,
            targetEcs,
            TargetAttribute.Hp,
            baseVal,
            ImpactOperationType.Subtract,
            impactType,
            sourceType);
    }

    /// <summary> ArgI0：位移模板 id；ArgF0/ArgF1/ArgF2：力度或与 Provider 向量相关的占位标量；ArgS：可调键。→ Motor / Knockback 管线。 </summary>
    private static void PlaceholderForcedDisplacement(
        BuffOpcodeInstruction ins,
        EcsEntity sourceEcs,
        EcsEntity targetEcs,
        EntityBase provider,
        EntityBase owner)
    {
        LogOpcodeStub(nameof(BuffEffectOpcode.ForcedDisplacement),
            $"targetEcs={targetEcs.Id} templateId={ins.ArgI0} f=({ins.ArgF0},{ins.ArgF1},{ins.ArgF2}) key={ins.ArgS} （待接 Motor/Knockback）");
        _ = sourceEcs;
        _ = provider;
        _ = owner;
    }

    /// <summary> ArgI0：路径/Waypath 资源 id；ArgS：表键 → NavMotorBridge.SetDestination意向。 </summary>
    private static void PlaceholderForcedPathfind(
        BuffOpcodeInstruction ins,
        EcsEntity sourceEcs,
        EcsEntity targetEcs,
        EntityBase provider,
        EntityBase owner)
    {
        LogOpcodeStub(nameof(BuffEffectOpcode.ForcedPathfind),
            $"targetEcs={targetEcs.Id} pathTemplateId={ins.ArgI0} key={ins.ArgS} （待写 ECS Waypoint意向 + NavBridge）");
        _ = sourceEcs;
        _ = provider;
        _ = owner;
    }

    /// <summary> ArgI0：<see cref="OperationLockMask"/> 组合移动/视野/普攻/技能禁用。→ 输入守门或 ECS 锁组件（待实现）。 </summary>
    private static void PlaceholderCharacterOperationLock(
        BuffOpcodeInstruction ins,
        EcsEntity targetEcs,
        EntityBase owner)
    {
        var mask = (OperationLockMask)ins.ArgI0;
        string flags = DescribeOperationLock(mask);
        LogOpcodeStub(nameof(BuffEffectOpcode.CharacterOperationLock),
            $"target={targetEcs.Id} masks=[{flags}] ArgF0={ins.ArgF0} （待接 InputGate / ECS OpLockComponent）");
        _ = owner;
    }

    private static string DescribeOperationLock(OperationLockMask mask)
    {
        if (mask == OperationLockMask.None)
            return nameof(OperationLockMask.None);
        var parts = "";
        void Add(OperationLockMask f, string name)
        {
            if ((mask & f) != 0)
                parts += string.IsNullOrEmpty(parts) ? name : "+" + name;
        }
        Add(OperationLockMask.Movement, "Move");
        Add(OperationLockMask.Vision, "Vision");
        Add(OperationLockMask.NormalAttack, "NormalAtk");
        Add(OperationLockMask.SkillCast, "Skill");
        return string.IsNullOrEmpty(parts) ? mask.ToString() : parts;
    }

    /// <summary> ArgI0：<see cref="CrowdControlMask"/>，与 Fear/Silence 等 CC UI 对齐，区别于 <see cref="CharacterOperationLock"/>。 </summary>
    private static void PlaceholderCrowdControlLock(BuffOpcodeInstruction ins, EcsEntity targetEcs, EntityBase owner)
    {
        var cc = (CrowdControlMask)ins.ArgI0;
        LogOpcodeStub(nameof(BuffEffectOpcode.ControlLock),
            $"target={targetEcs.Id} CrowdControlMask={cc} （待 CC 总管 / UI）");
        _ = owner;
    }

    private static void PlaceholderStatModify(BuffOpcodeInstruction ins, EcsEntity targetEcs, EntityBase owner)
    {
        LogOpcodeStub(ins.Opcode.ToString(),
            $"target={targetEcs.Id} ArgI0={ins.ArgI0} ArgI1={ins.ArgI1} ArgF0={ins.ArgF0} （待 EntityData / Impact 属性通路）");
        _ = owner;
    }

    private static void PlaceholderApplyChildBuff(BuffOpcodeInstruction ins, EntityBase provider, EntityBase owner)
    {
        LogOpcodeStub(nameof(BuffEffectOpcode.ApplyChildBuffById),
            $"childBuff 解析自定 ArgI0/ArgS={ins.ArgS} provider={provider?.EntityId} owner={owner?.EntityId} （待 BuffApplyService）");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private static void LogOpcodeStub(string tag, string detail)
    {
        Debug.Log($"[BuffOpcodeDispatcher][占位] {tag}: {detail}");
    }
}
