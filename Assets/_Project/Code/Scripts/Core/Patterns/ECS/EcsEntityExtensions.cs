namespace Core.ECS
{
    /// <summary>
    /// 便于战斗/Impact 等处的 <see cref="EcsEntity"/> 链式访问（与 Mono 桥的 <see cref="EcsEntityBridge"/> 分离）。
    /// </summary>
    public static class EcsEntityExtensions
    {
        public static bool IsValid(this EcsEntity entity)
        {
            return entity.Id != 0 && EcsWorld.Exists(entity);
        }

        public static bool HasComponent<T>(this EcsEntity entity) where T : struct, IEcsComponent
            => EcsWorld.HasComponent<T>(entity);

        public static T GetComponent<T>(this EcsEntity entity) where T : struct, IEcsComponent
            => EcsWorld.GetComponent<T>(entity);

        public static void SetComponent<T>(this EcsEntity entity, T component) where T : struct, IEcsComponent
            => EcsWorld.SetComponent(entity, component);

        public static void AddComponent<T>(this EcsEntity entity, T component) where T : struct, IEcsComponent
            => EcsWorld.AddComponent(entity, component);

        public static void RemoveComponent<T>(this EcsEntity entity) where T : struct, IEcsComponent
            => EcsWorld.RemoveComponent<T>(entity);
    }
}
