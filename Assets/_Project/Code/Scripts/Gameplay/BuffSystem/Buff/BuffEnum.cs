using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Buff / Debuff UI 或驱散标签归类。 </summary>
public enum BuffType
{
    Buff,
    Debuff,
    None,
}

public enum BuffConflictResolution
{
    Combine,
    Separate,
    Cover,
}

/// <summary>
/// 原子效果操作码。<br/>
/// 一条 Buff 在数据侧由<strong>多条 <see cref="BuffOpcodeInstruction"/> / <see cref="BuffEffectComposition"/></strong> 或由 <see cref="BuffOpcodeRecipe"/> 派生配方组合而成；<strong>不再</strong>使用扁平巨型 <c>BuffEffect</c> 枚举。
/// </summary>
public enum BuffEffectOpcode
{
    None = 0,

    /// <summary> 经由 Impact 结算的伤害（字段含义由 Dispatcher 解析 <see cref="BuffOpcodeInstruction"/>）。 </summary>
    ImpactDamage = 10,

    /// <summary> 修改 <see cref="Core.Entity.EntityBaseDataCore"/> 类属性。 ArgI0=枚举序，ArgF0=delta，ArgI1 可存操作类型。 </summary>
    StatModifyCore = 20,

    /// <summary> 修改 <see cref="Core.Entity.EntityBaseData"/> 类属性。 </summary>
    StatModifyBonus = 21,

    /// <summary> 群体控制 bitmask，见 <see cref="CrowdControlMask"/>。 </summary>
    ControlLock = 30,

    /// <summary> 强制位移意向（力度/方向模板 id 等，由 Motor 桥解释）。 </summary>
    ForcedDisplacement = 40,

    /// <summary> 再施加另一条 Buff（buffId）。 </summary>
    ApplyChildBuffById = 50,
}

/// <summary> CC  bitmask；与 <see cref="BuffEffectOpcode.ControlLock"/> 的 ArgI0 搭配。毕设可先只用子集。 </summary>
[System.Flags]
public enum CrowdControlMask
{
    None = 0,
    Silence = 1 << 0,
    Disarm = 1 << 1,
    Root = 1 << 2,
    Taunt = 1 << 3,
    Fear = 1 << 4,
    Charm = 1 << 5,
    Stun = 1 << 6,
}
