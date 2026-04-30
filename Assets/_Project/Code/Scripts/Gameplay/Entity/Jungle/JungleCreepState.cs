namespace Core.Entity.Jungle
{
    /// <summary> 野区单位 AI 状态；《MOBA局内单位模块ECS设计文档》 §7。 </summary>
    public enum JungleCreepState : byte
    {
        Idle = 0,
        Pursue = 1,
        Returning = 2,
    }
}
