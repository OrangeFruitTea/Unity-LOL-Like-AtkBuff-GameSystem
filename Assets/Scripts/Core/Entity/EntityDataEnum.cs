namespace Core.Entity
{
    public enum EntityBaseDataCore
    {
        HpLimit,
        CrtHp,
        MpLimit,
        CrtMp,
        AtkAD,
        AtkAP,
        DefenceAD,
        DefenceAP,
        AtkSpeed,
        SkillCd,
        CriticalRate,
        MoveSpeed,
    }

    public enum EntityBaseData
    {
        HpRecoverPerSecond,
        MpRecoverPerSecond,
        // 护甲穿透
        PenAD,
        PenAP,
        LifeSteal,
        // 全能吸血
        OmniVamp,
        AtkDistance,
        CriticalDamage,
        // 韧性
        Resilience,
    }
}