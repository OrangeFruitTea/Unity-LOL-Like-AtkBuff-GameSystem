using UnityEngine;

namespace Core.Combat
{
    public enum ImpactType 
    {
        Physical,
        Magical,
        True,
    }

    public enum ImpactSourceType
    {
        NormalAtk,
        Skill,
        Buff,
        Environment,
    }

    public enum ImpactOperationType
    {
        Add,
        Subtract,
        Override,
    }

    public enum TargetAttribute
    {
        Hp,
        Mp,
        AtkAD,
        AtkAP,
        DefenceAD,
        DefenceAP,
        AtkSpeed,
        SkillCd,
        CriticalRate,
        MoveSpeed,
        HpRecoverPerSecond,
        MpRecoverPerSecond,
        PenAD,
        PenAP,
        LifeSteal,
        OmniVamp,
        AtkDistance,
        CriticalDamage,
        Resilience,
        Shield,
        Stunned,
        Silenced,
        Rooted,
        Snared,
        Invulnerable,
        Invisible,
    }

    public enum ModifierPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3,
    }

    public enum HitResult
    {
        Hit,
        Miss,
        Dodge,
        Resist,
    }
}

