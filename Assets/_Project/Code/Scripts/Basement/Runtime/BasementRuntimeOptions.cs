namespace Basement.Runtime
{
    /// <summary>
    /// Basement 与 Unity 主循环的集成选项；可在游戏入口覆盖默认值。
    /// </summary>
    public static class BasementRuntimeOptions
    {
        /// <summary>
        /// 为真时，在首次场景加载后向 <see cref="Core.ECS.EcsWorld"/> 注册 <see cref="BasementPumpEcsSystem"/>，
        /// 由 ECS 主循环驱动 MatchTime / TimingTask / GameEvent 调度，<see cref="Basement.Tasks.Unity.TaskDispatcher"/> 与
        /// <see cref="Basement.Events.Unity.EventDispatcher"/> 的 <c>Update</c> 将自动跳过泵送以避免重复。
        /// </summary>
        public static bool UseEcsWorldBasementLoop { get; set; } = true;
    }
}
