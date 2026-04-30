using System.Collections.Generic;
using Core.Combat;

/// <summary>
/// MVP：按 buffId 提供 <see cref="BuffEffectComposition"/> 与可选周期间隔（秒）。后续可改由 JSON 反序列化。
/// </summary>
public static class BuffOpcodeMvpDefinitions
{
    /// <summary> 施加即一次魔法伤害（Skill），基数 ArgF0；伤害 × CurrentLevel。 </summary>
    public const int InstantMagicDamageTest = 90001;

    /// <summary> 无 OnApply 伤害；按 interval 周期魔法伤害（Skill）。 </summary>
    public const int SimplePeriodicMagicDotTest = 90002;

    public static bool TryGetComposition(int buffId, out BuffEffectComposition composition)
    {
        switch (buffId)
        {
            case InstantMagicDamageTest:
                composition = BuildInstantDamage(25f, ImpactType.Magical, ImpactSourceType.Skill);
                return true;
            case SimplePeriodicMagicDotTest:
                composition = BuildPeriodicOnly(8f, ImpactType.Magical, ImpactSourceType.Skill);
                return true;
            default:
                composition = null;
                return false;
        }
    }

    /// <returns> &lt; 0 表示不跑周期 opcode。 </returns>
    public static float GetPeriodicIntervalSeconds(int buffId)
    {
        return buffId == SimplePeriodicMagicDotTest ? 1f : -1f;
    }

    private static BuffEffectComposition BuildInstantDamage(
        float baseDamage,
        ImpactType impactType,
        ImpactSourceType sourceType)
    {
        var c = new BuffEffectComposition();
        c.OnApply = new List<BuffOpcodeInstruction>
        {
            new BuffOpcodeInstruction
            {
                Opcode = BuffEffectOpcode.ImpactDamage,
                ArgF0 = baseDamage,
                ArgI0 = (int)impactType,
                ArgI1 = (int)sourceType,
            }
        };
        return c;
    }

    private static BuffEffectComposition BuildPeriodicOnly(
        float damagePerTick,
        ImpactType impactType,
        ImpactSourceType sourceType)
    {
        var c = new BuffEffectComposition();
        c.OnPeriodicTick = new List<BuffOpcodeInstruction>
        {
            new BuffOpcodeInstruction
            {
                Opcode = BuffEffectOpcode.ImpactDamage,
                ArgF0 = damagePerTick,
                ArgI0 = (int)impactType,
                ArgI1 = (int)sourceType,
            }
        };
        return c;
    }
}
