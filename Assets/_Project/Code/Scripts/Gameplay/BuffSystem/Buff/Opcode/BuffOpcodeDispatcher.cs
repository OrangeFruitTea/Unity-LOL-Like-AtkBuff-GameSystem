using Core.Combat;
using Core.ECS;
using Core.Gameplay;
using Core.Entity;
using UnityEngine;

/// <summary>
/// Opcode → Impact / （后续 StatModify/CC）。MVP：仅处理 <see cref="BuffEffectOpcode.ImpactDamage"/>。
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

        var impacts = new ImpactManager(world);
        uint stacks = runtimeData != null ? System.Math.Max(1u, runtimeData.CurrentLevel) : 1u;

        foreach (var ins in instructions)
        {
            switch (ins.Opcode)
            {
                case BuffEffectOpcode.ImpactDamage:
                    float baseVal = ins.ArgF0 * stacks;
                    var impactType = (ImpactType)ins.ArgI0;
                    if (!System.Enum.IsDefined(typeof(ImpactType), impactType))
                        impactType = ImpactType.Magical;
                    var sourceType = (ImpactSourceType)ins.ArgI1;
                    if (!System.Enum.IsDefined(typeof(ImpactSourceType), sourceType))
                        sourceType = ImpactSourceType.Skill;

                    impacts.CreateImpactEvent(
                        sourcesEcs,
                        targetEcs,
                        TargetAttribute.Hp,
                        baseVal,
                        ImpactOperationType.Subtract,
                        impactType,
                        sourceType);
                    break;

                case BuffEffectOpcode.None:
                    break;

                default:
                    Debug.LogWarning($"[BuffOpcodeDispatcher] MVP 未实现 opcode={ins.Opcode}");
                    break;
            }
        }
    }
}
