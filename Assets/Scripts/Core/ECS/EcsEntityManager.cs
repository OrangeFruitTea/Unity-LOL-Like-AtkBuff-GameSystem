using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.ECS
{
    public class EcsEntityManager
    {
        private long _nextEntityId = 0;
        // 实体-组件映射: 实体ID->组件类型->组件实例
        private Dictionary<long, Dictionary<Type, IEcsComponent>> _entityComponents = new();
        // 组件-实体映射: 组件类型->拥有该组件的所有实体ID列表
        private Dictionary<Type, HashSet<long>> _componentEntities = new();

        public EcsEntity CreateEntity()
        {
            var entity = new EcsEntity(_nextEntityId++);
            _entityComponents[entity.Id] = new Dictionary<Type, IEcsComponent>();
            return entity;
        }

        public void DestroyEntity(EcsEntity entity)
        {
            if (!_entityComponents.ContainsKey(entity.Id)) return;
            var components = _entityComponents[entity.Id];
            foreach (var component in components.Keys.ToList())
            {
                RemoveComponent(entity, component);
            }

            _entityComponents.Remove(entity.Id);
        }

        public bool Exists(EcsEntity entity)
        {
            return _entityComponents.ContainsKey(entity.Id);
        }

        public void AddComponent<T>(EcsEntity entity, T component) where T : struct, IEcsComponent
        {
            var type = typeof(T);
            if (!_entityComponents.ContainsKey(entity.Id)) throw new ArgumentException($"实体{entity.Id}不存在");
            if (_entityComponents[entity.Id].ContainsKey(type)) RemoveComponent(entity, type);
            _entityComponents[entity.Id][type] = component;
            if (!_componentEntities.ContainsKey(type))
                _componentEntities[type] = new HashSet<long>();
            _componentEntities[type].Add(entity.Id);

        }

        public void RemoveComponent<T>(EcsEntity entity) where T : struct, IEcsComponent
        {
            RemoveComponent(entity, typeof(T));
        }

        private void RemoveComponent(EcsEntity entity, Type componentType)
        {
            if (!_entityComponents.ContainsKey(entity.Id)) return;
            if (!_entityComponents[entity.Id].ContainsKey(componentType)) return;
            _entityComponents[entity.Id].Remove(componentType);
            if (!_componentEntities.ContainsKey(componentType)) return;
            _componentEntities[componentType].Remove(entity.Id);
            if (_componentEntities[componentType].Count == 0)
                _componentEntities.Remove(componentType);
        }

        public bool HasComponent<T>(EcsEntity entity) where T :struct, IEcsComponent
        {
            return _entityComponents.TryGetValue(entity.Id, out var components) &&
                   components.ContainsKey(typeof(T));
        }

        public T GetComponent<T>(EcsEntity entity) where T : struct, IEcsComponent
        {
            if (!_entityComponents.TryGetValue(entity.Id, out var components) ||
                !components.TryGetValue(typeof(T), out var component))
            {
                throw new InvalidOperationException($"实体{entity.Id}没有组件{typeof(T).Name}");
            }
            return (T)component;
        }

        // 用于更新组件
        public void SetComponent<T>(EcsEntity entity, T component) where T : struct, IEcsComponent
        {
            var componentType = typeof(T);
            if (!_entityComponents.TryGetValue(entity.Id, out var components) ||
                !components.ContainsKey(componentType))
            {
                throw new InvalidOperationException($"实体{entity.Id}没有组件{typeof(T).Name}");
            }

            components[componentType] = component;
        }

        public IEnumerable<EcsEntity> GetEntitiesWithComponent<T>() where T : struct, IEcsComponent
        {
            var componentType = typeof(T);
            if (!_componentEntities.TryGetValue(componentType, out var entityIds))
                return Enumerable.Empty<EcsEntity>();
            return entityIds.Select(id => new EcsEntity(id));
        }

        public IEnumerable<EcsEntity> GetEntitiesWithAll(params Type[] componnetTypes)
        {
            if (componnetTypes.Length == 0) return Enumerable.Empty<EcsEntity>();
            if (!_componentEntities.TryGetValue(componnetTypes[0], out var baseEntities))
                return Enumerable.Empty<EcsEntity>();
            var result = new HashSet<long>(baseEntities);
            foreach (var type in componnetTypes.Skip(1))
            {
                if (!_componentEntities.TryGetValue(type, out var entitiesWithType))
                    return Enumerable.Empty<EcsEntity>();
                result.IntersectWith(entitiesWithType);
                if (result.Count == 0)
                {
                    return Enumerable.Empty<EcsEntity>();
                }
            }
            return result.Select(id => new EcsEntity(id));
        }
    }
}
