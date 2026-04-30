namespace Core.Entity
{
    /// <summary> 阵营 id；寻敌与兵线敌对关系用。与设计文档 《MOBA局内单位模块ECS设计文档》 §4 一致。 </summary>
    public enum FactionTeamId : byte
    {
        Neutral = 0,
        Blue = 1,
        Red = 2,
    }
}
