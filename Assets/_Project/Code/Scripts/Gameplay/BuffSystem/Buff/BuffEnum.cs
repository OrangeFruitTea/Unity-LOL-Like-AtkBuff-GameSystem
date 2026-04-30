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

    /// <summary> 强制位移（击退/击退拉等），字段由 Dispatcher 占位解析；Motor / Rigidbody / Nav 桥待定。 ArgI0=模板 id；ArgF0/ArgF1=力度分量等。 </summary>
    ForcedDisplacement = 40,

    /// <summary> 强制寻路（锁目标点/路径资源），字段占位；ECS 仅存意向，<c>NavMeshAgent</c> 由 Prefab 桥驱动。 ArgI0=路径/Waypath 模板 id；ArgS=可调配置键。 </summary>
    ForcedPathfind = 41,

    /// <summary> 操作封锁：移动 / 视野 / 普攻 / 技能 由 <see cref="OperationLockMask"/>（ArgI0）组合指定；占位，待 ECS 读写或输入守门。 </summary>
    CharacterOperationLock = 42,

    /// <summary> 再施加另一条 Buff（buffId）。 </summary>
    ApplyChildBuffById = 50,
}

/// <summary>
/// 「操作锁定」细粒度 bitmask，与 <see cref="BuffEffectOpcode.CharacterOperationLock"/> 的 <c>ArgI0</c> 搭配。<br/>
/// 与 <see cref="CrowdControlMask"/> 正交（后者偏「状态机/CC 标签」，本枚举偏输入与战斗通道封锁）。
/// </summary>
[System.Flags]
public enum OperationLockMask : int
{
    None = 0,
    /// <summary> 禁止主动移动指令（Joystick / 点击移动等）。 </summary>
    Movement = 1 << 0,
    /// <summary> 禁止或遮挡视野技能/旋转视野（Fog/Blur/镜头锁，实现待定）。 </summary>
    Vision = 1 << 1,
    /// <summary> 禁止普攻指令。 </summary>
    NormalAttack = 1 << 2,
    /// <summary> 禁止释放技能指令。 </summary>
    SkillCast = 1 << 3,
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
