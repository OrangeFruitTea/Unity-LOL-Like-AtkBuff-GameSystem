namespace Core.Entity.Minions
{
    /// <summary> 兵线重寻路原因；设计文档 §6。 </summary>
    public enum LaneMinionRepathReason : byte
    {
        None = 0,
        PathBlocked = 1,
        ShovedAside = 2,
        RouteChangedByGame = 3,
    }
}
