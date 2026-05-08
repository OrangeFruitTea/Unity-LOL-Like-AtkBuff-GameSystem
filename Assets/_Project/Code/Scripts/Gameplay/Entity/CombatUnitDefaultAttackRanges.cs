namespace Core.Entity
{
    /// <summary>
    /// 按 <see cref="UnitArchetype"/> 在生成时写入 <see cref="EntityBaseData.AtkDistance"/> 的基准值（世界单位）。<br/>
    /// 需搭配 <see cref="CombatEntitySpawnProfile.AddArchetype"/>；无 Profile 或关闭 Archetype 时仍用 <see cref="EntityDataComponent.InitializeDefaults"/>。
    /// </summary>
    public static class CombatUnitDefaultAttackRanges
    {
        public const float Hero = 250f;
        public const float LaneMinionAndJungle = 100f;
        public const float Tower = 350f;

        public static void ApplyArchetype(ref EntityDataComponent data, UnitArchetype archetype)
        {
            if (TryGetAttackDistance(archetype, out var d))
                data.SetData(EntityBaseData.AtkDistance, d);
        }

        public static bool TryGetAttackDistance(UnitArchetype archetype, out double distance)
        {
            switch (archetype)
            {
                case UnitArchetype.Hero:
                    distance = Hero;
                    return true;
                case UnitArchetype.LaneMinion:
                case UnitArchetype.JungleMonster:
                case UnitArchetype.EpicMonster:
                    distance = LaneMinionAndJungle;
                    return true;
                case UnitArchetype.Tower:
                    distance = Tower;
                    return true;
                default:
                    distance = default;
                    return false;
            }
        }
    }
}
