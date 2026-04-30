namespace Core.Entity
{
    /// <summary> 简易索敌模式（塔表）；实现可删减。 </summary>
    public enum TowerTargetingMode : byte
    {
        MinionsFirst = 0,
        HeroesFirst = 1,
        NearestThreat = 2,
    }
}
