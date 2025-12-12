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

        public void AddPendingEntity(EntityBase entity)
        {
            if (entity != null)
            {
                _pendingEntities.Enqueue(entity);
            }
        }

        private void SpawnEcsEntity(EntityBase sceneEntity)
        {
            var ecsEntity = EcsWorld.Instance.EcsManager.CreateEntity();
            sceneEntity.BoundEcsEntity = ecsEntity;
            sceneEntity.entityBridge.BoundEcsEntity = ecsEntity;
            AddBaseComponents(ecsEntity, sceneEntity);
            Debug.Log($"SpawnSystem创建ECS实体[ID: {ecsEntity.Id}]，关联场景实体: [{sceneEntity.EntityId}]");
        }

        private void AddBaseComponents(EcsEntity ecsEntity, EntityBase sceneEntity)
        {
            var dataComp = new EntityDataComponent();
            dataComp.InitializeDefaults();
            EcsWorld.AddComponent(ecsEntity, dataComp);
        }

        private void ProcessDestroyRequests()
        {
        }
    }
}