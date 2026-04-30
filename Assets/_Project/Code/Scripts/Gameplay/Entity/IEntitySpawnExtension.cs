using Core.ECS;

namespace Core.Entity
{
    /// <summary>
    /// 在 <see cref="EntitySpawnSystem"/> 写好 <see cref="EntityDataComponent"/> 与可选 Profile，
    /// 并完成 <see cref="EntityEcsLinkRegistry.Register"/> 之后调用；用于挂载塔 / 兵线 / 野怪等专精组件（设计文档 §9）。
    /// </summary>
    public interface IEntitySpawnExtension
    {
        void OnAfterEcsBaseSpawned(EcsEntity ecs, EntityBase host);
    }
}
