using Core.ECS;
using UnityEngine;

namespace Core.Entity
{
    public class EntityBase : MonoBehaviour
    {
        private long _entityId = 12345;
        private string _entityName = "testEntity";
        // ECS核心，关联的ECS实体
        public EcsEntity BoundEcsEntity;
        public EcsEntityBridge entityBridge;

        protected virtual void Awake()
        {
            entityBridge = GetComponent<EcsEntityBridge>();
            if (entityBridge == null)
            {
                entityBridge = gameObject.AddComponent<EcsEntityBridge>();
            }
        }

        protected void OnDestroy()
        {
            if (BoundEcsEntity.Id != 0 
                && EcsWorld.Instance != null
                && EcsWorld.Instance.EcsManager != null)
                EcsWorld.Instance.EcsManager.DestroyEntity(BoundEcsEntity);
        }

        public long EntityId
        {
            get => _entityId;
        }
    
        public string EntityName
        {
            get => _entityName;
            set => _entityName = value;
        }
    }
}
