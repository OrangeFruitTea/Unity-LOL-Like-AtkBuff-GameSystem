namespace Basement.Json
{
    /// <summary>
    /// <see cref="JsonManager"/> 中多套路由序列化策略的键。
    /// </summary>
    public enum JsonSerializerProfile
    {
        /// <summary> 与历史行为一致：持久化 / 通用。 </summary>
        RuntimePersistence = 0,

        /// <summary> 游戏内容表（技能、Buff 等 JSON）。 </summary>
        GameContent = 1
    }
}
