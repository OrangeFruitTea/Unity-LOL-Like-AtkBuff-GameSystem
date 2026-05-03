using System.Collections.Generic;
using Core.ECS;
using UnityEngine;

namespace Core.Entity
{
    public class EntitySpawnSystem  : IEcsSystem
    {
        // 场景中等待初始化的EntityBase
        private Queue<EntityBase> _pendingEntities = new Queue<EntityBase>();
        // 每帧处理待初始化的实体
        public int UpdateOrder => 5;

        public void Update()
        {
            while (_pendingEntities.Count > 0)
            {
                var sceneEntity = _pendingEntities.Dequeue();
                SpawnEcsEntity(sceneEntity);
            }
            ProcessDestroyRequests();
        }

        public void Initialize()
        {
        }

        public void Destroy()
        {
            _pendingEntities.Clear();
        }

        /// <summary>
        /// 立即处理队列内所有待生成实体（与 <see cref="Update"/> 逻辑相同）。用于避免协程与 <see cref="EcsWorld.Update"/> 顺序导致的长时间等待。
        /// </summary>
        public void FlushPendingEntitiesNow()
        {
            while (_pendingEntities.Count > 0)
            {
                var sceneEntity = _pendingEntities.Dequeue();
                SpawnEcsEntity(sceneEntity);
            }

            ProcessDestroyRequests();
        }

        public void AddPendingEntity(EntityBase entity)
        {
            if (entity != null)
            {
                _pendingEntities.Enqueue(entity);
            }
        }

        private void SpawnEcsEntity(EntityBase sceneEntity)
        {
            if (sceneEntity.entityBridge == null)
            {
                sceneEntity.entityBridge = sceneEntity.GetComponent<EcsEntityBridge>();
                if (sceneEntity.entityBridge == null)
                    sceneEntity.entityBridge = sceneEntity.gameObject.AddComponent<EcsEntityBridge>();
            }

            var ecsEntity = EcsWorld.Instance.EcsManager.CreateEntity();
            sceneEntity.BoundEcsEntity = ecsEntity;
            sceneEntity.entityBridge.BoundEcsEntity = ecsEntity;
            AddBaseComponents(ecsEntity, sceneEntity);
            EntityEcsLinkRegistry.Register(sceneEntity);
            RunSpawnExtensions(ecsEntity, sceneEntity);
            Debug.Log($"SpawnSystem创建ECS实体[ID: {ecsEntity.Id}]，关联场景实体: [{sceneEntity.EntityId}]");
        }

        private void AddBaseComponents(EcsEntity ecsEntity, EntityBase sceneEntity)
        {
            var dataComp = new EntityDataComponent();
            dataComp.InitializeDefaults();
            EcsWorld.AddComponent(ecsEntity, dataComp);

            var profile = sceneEntity.GetComponent<CombatEntitySpawnProfile>();
            if (profile == null)
                return;

            if (profile.AddFaction)
            {
                var fac = new FactionComponent();
                fac.InitializeDefaults();
                fac.TeamId = profile.TeamId;
                EcsWorld.AddComponent(ecsEntity, fac);
            }

            if (profile.AddArchetype)
            {
                var arch = new UnitArchetypeComponent();
                arch.InitializeDefaults();
                arch.Archetype = profile.Archetype;
                arch.ConfigId = profile.ConfigId;
                EcsWorld.AddComponent(ecsEntity, arch);
            }

            if (profile.AddCombatBoardLite)
            {
                var board = new CombatBoardLiteComponent();
                board.InitializeDefaults();
                EcsWorld.AddComponent(ecsEntity, board);
            }
        }

        private static void RunSpawnExtensions(EcsEntity ecsEntity, EntityBase sceneEntity)
        {
            if (sceneEntity == null)
                return;

            foreach (var mb in sceneEntity.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null || !mb.enabled)
                    continue;

                if (mb is IEntitySpawnExtension ext)
                    ext.OnAfterEcsBaseSpawned(ecsEntity, sceneEntity);
            }
        }

        private void ProcessDestroyRequests()
        {
        }
    }
}