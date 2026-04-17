using System.Collections.Generic;
using Core.ECS;
using UnityEngine;

namespace Core.Entity
{
    /// <summary>
    /// <see cref="EcsEntity.Id"/> 与场景侧 <see cref="EntityBase"/> 的注册表，供 Buff / 技能管线等从 ECS 实体反查 Mono 宿主。
    /// 在 <see cref="EntitySpawnSystem"/> 生成 ECS 实体后注册，在 <see cref="EntityBase"/> 销毁时注销。
    /// </summary>
    public static class EntityEcsLinkRegistry
    {
        private static readonly Dictionary<long, EntityBase> ByEcsId = new Dictionary<long, EntityBase>();

        public static void Register(EntityBase entity)
        {
            if (entity == null)
                return;
            long id = entity.BoundEcsEntity.Id;
            if (id == 0)
                return;
            ByEcsId[id] = entity;
        }

        public static void Unregister(EntityBase entity)
        {
            if (entity == null)
                return;
            long id = entity.BoundEcsEntity.Id;
            if (id == 0)
                return;
            if (ByEcsId.TryGetValue(id, out var current) && current == entity)
                ByEcsId.Remove(id);
        }

        public static bool TryGetEntityBase(EcsEntity ecsEntity, out EntityBase entity)
        {
            entity = null;
            if (ecsEntity.Id == 0)
                return false;
            if (!ByEcsId.TryGetValue(ecsEntity.Id, out entity))
                return false;
            // Unity 已销毁的 MonoBehaviour：引用非空但 == null
            if (entity == null)
                return false;
            return true;
        }

        /// <summary> ECS 实体仍在管理器中且能解析到未销毁的 <see cref="EntityBase"/>。 </summary>
        public static bool IsLinkedAlive(EcsEntity ecsEntity)
        {
            if (!EcsWorld.Exists(ecsEntity))
                return false;
            if (!TryGetEntityBase(ecsEntity, out var eb))
                return false;
            return eb.gameObject.activeInHierarchy;
        }
    }
}
