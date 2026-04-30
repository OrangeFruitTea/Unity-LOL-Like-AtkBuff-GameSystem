using System;
using System.Collections.Generic;
using Core.Combat;
using Core.Entity;

/// <summary>
/// 一条可序列化的 opcode 指令（表驱动 JSON / ScriptableObject 与代码共用形状）。参数语义随 <see cref="BuffEffectOpcode"/> 变化，未用字段应为 0。
/// </summary>
[Serializable]
public sealed class BuffOpcodeInstruction
{
    public BuffEffectOpcode Opcode = BuffEffectOpcode.None;

    /// <summary>通用浮点参数（如伤害基数、比例）。</summary>
    public float ArgF0;

    public float ArgF1;

    public float ArgF2;

    /// <summary>通用整型参数（如 ControlLock 时用 <see cref="CrowdControlMask"/>）。</summary>
    public int ArgI0;

    public int ArgI1;

    /// <summary>如子 Buff id、配置表键等。</summary>
    public string ArgS;
}

/// <summary>
/// 「多重 opcode」数据组合：挂载在 Buff JSON 或与 <see cref="BuffOpcodeRecipe"/> 互转。<br/>
/// 触发时机（Apply / Periodic / Remove）由外层 profile 绑定，参见 Opcode 设计文档。
/// </summary>
[Serializable]
public sealed class BuffEffectComposition
{
    public List<BuffOpcodeInstruction> OnApply = new();

    public List<BuffOpcodeInstruction> OnPeriodicTick = new();

    public List<BuffOpcodeInstruction> OnRemove = new();
}

/// <summary>
/// 代码侧可扩展的<strong> opcode 配方</strong>：子类<strong>内含</strong>一组或多组指令的合成规则（可多 opcode）。<br/>
/// 运行时通常把结果缓存进 <see cref="BuffEffectComposition"/> 或由 Dispatcher 逐帧调用。
/// </summary>
public abstract class BuffOpcodeRecipe
{
    /// <returns>本条配方在给定等级下的指令序列（可多 opcode）。</returns>
    public abstract IReadOnlyList<BuffOpcodeInstruction> BuildInstructions(uint buffLevel);
}

/// <summary> 静态列表封装，便于不写子类即用 <see cref="BuffEffectComposition"/> 填表。</summary>
public sealed class BuffOpcodeListRecipe : BuffOpcodeRecipe
{
    public readonly List<BuffOpcodeInstruction> Instructions = new();

    public override IReadOnlyList<BuffOpcodeInstruction> BuildInstructions(uint buffLevel)
    {
        _ = buffLevel;
        return Instructions;
    }
}

/// <summary> 示例配方：周期性伤害意图（ Dispatcher 须在 Tick 时使用 OnPeriodicTick 列表调用）。仅作文档级样板，可调参。 </summary>
public sealed class PoisonPeriodicDamageRecipe : BuffOpcodeRecipe
{
    public float DamagePerTick = 10f;

    public override IReadOnlyList<BuffOpcodeInstruction> BuildInstructions(uint buffLevel)
    {
        float v = DamagePerTick * Math.Max(1u, buffLevel);
        return new[]
        {
            new BuffOpcodeInstruction
            {
                Opcode = BuffEffectOpcode.ImpactDamage,
                ArgF0 = v,
                ArgI0 = (int)ImpactType.Magical
            }
        };
    }
}

/// <summary> 示例配方：恐惧类 = 多重 opcode（控制锁 + 强制位移示意 + 移速减值示意）。 Interpreter 未完成前仅为结构示范。 </summary>
public sealed class FearMovementLockRecipe : BuffOpcodeRecipe
{
    public float MoveSpeedPenaltyPercent = 0.25f;

    public override IReadOnlyList<BuffOpcodeInstruction> BuildInstructions(uint buffLevel)
    {
        _ = buffLevel;
        int cc = (int)(CrowdControlMask.Fear | CrowdControlMask.Disarm | CrowdControlMask.Silence);
        return new[]
        {
            new BuffOpcodeInstruction
            {
                Opcode = BuffEffectOpcode.ControlLock,
                ArgI0 = cc,
            },
            new BuffOpcodeInstruction
            {
                Opcode = BuffEffectOpcode.ForcedDisplacement,
                ArgF0 = 1f,
                ArgI0 = 1,
            },
            new BuffOpcodeInstruction
            {
                Opcode = BuffEffectOpcode.StatModifyBonus,
                ArgI0 = (int)EntityBaseData.MoveSpeed,
                ArgF0 = -MoveSpeedPenaltyPercent,
            }
        };
    }
}
