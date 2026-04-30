namespace Core.Entity
{
    /// <summary> 逻辑单位原型；具体数值变种用 <see cref="UnitArchetypeComponent.ConfigId"/>。</summary>
    public enum UnitArchetype : byte
    {
        Hero = 0,
        LaneMinion = 1,
        JungleMonster = 2,
        Tower = 3,
        EpicMonster = 4,
    }
}
