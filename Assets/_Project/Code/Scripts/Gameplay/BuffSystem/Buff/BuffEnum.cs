using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
public enum BuffEffect
{
    None,
    // 修改数值
    ChangeHp,
    ChangeHpLimit,
    ChangeHpRecoverPerSecond,
    ChangeMp,
    ChangeMpLimit,
    ChangeMpRecoverPerSecond,
    ChangeAtkAD,
    ChangeAtkAP,
    ChangeDefenceAD,
    ChangeDefenceAP,
    ChangePenAD,
    ChangePenAP,
    ChangeLifeSteal,
    ChangeOmniVamp,
    ChangeSkillCd,
    ChangeMoveSpeed,
    ChangeAtkDistance,
    ChangeAtkSpeed,
    ChangeCriticalRate,
    ChangeCriticalDamage,
    // 控制
    // 混乱
    Confuse,
    // 沉默
    Silence,
    // 击退
    KnockDown,
    // 隐身
    Stealth,
    // 眩晕
    Stun,
    // 恐惧
    Fear,
    // 魅惑
    Charm,
    // 致盲
    Blind,
    // 嘲讽
    Taunt,
    // 禁锢
    Lock,
    // 缴械
    Disarm,
    TriggerAnotherBuff,
}
