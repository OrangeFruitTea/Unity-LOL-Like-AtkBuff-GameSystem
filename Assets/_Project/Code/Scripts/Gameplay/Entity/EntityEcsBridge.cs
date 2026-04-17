using Core.ECS;
using UnityEngine;

namespace Core.Entity
{
    /// <summary>
    /// <see cref="EntityBase"/> / <see cref="EcsEntity"/> 互查与存活判断的便捷入口（供 Buff、技能、战斗层使用）。
    /// </summary>
    public static class EntityEcsBridge
    {
        public static bool TryGetEntityBase(EcsEntity ecsEntity, out EntityBase entity) =>
            EntityEcsLinkRegistry.TryGetEntityBase(ecsEntity, out entity);

        public static EcsEntity GetEcsEntityOrDefault(EntityBase entity) =>
            entity != null ? entity.BoundEcsEntity : default;

        /// <summary> ECS 侧仍存在且能解析到有效宿主（用于施法前是否可对目标 <see cref="BuffManager.AddBuff"/>）。 </summary>
        public static bool IsValidBuffTarget(EcsEntity ecsEntity) =>
            EntityEcsLinkRegistry.IsLinkedAlive(ecsEntity);

        public static bool IsValidBuffTarget(EntityBase entity) =>
            entity != null
            && entity.BoundEcsEntity.Id != 0
            && EcsWorld.Exists(entity.BoundEcsEntity)
            && entity.gameObject.activeInHierarchy;
    }
}
