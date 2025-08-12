using System;
using System.Collections.Generic;
using System.Linq;
using Core.Entity;
using UnityEngine;
using utils;

namespace Core.ECS
{
    public static class EcsWorldInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void AutoCreateEcsWorld()
        {
            var _ = EcsWorld.Instance;
        }
    }
    public class EcsWorld : Singleton<EcsWorld>
    {
        public EcsEntityManager EcsManager { get; private set; }
        // List应保持有序
        private readonly List<IEcsSystem> _systems = new List<IEcsSystem>();
        private readonly List<Action> _sortedUpdateDelegates = new List<Action>();

        protected EcsWorld()
        {
        }

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        protected override void OnDestroy()
        {
            foreach (var system in _systems)
            {
                system.Destroy();
            }
            _systems.Clear();
            _sortedUpdateDelegates.Clear();
            base.OnDestroy();
        }

        public T GetEcsSystem<T>() where T :class, IEcsSystem
        {
            foreach (var system in _systems)
            {
                if (system is T target)
                    return target;
            }
            Debug.LogWarning($"未找到类型 {typeof(T).Name} 的Ecs系统, 需确保使用AddEcsSystem()显式添加");
            return null;
        }

        public void AddEcsSystem(IEcsSystem system)
        {
            if (system == null)
            {
                Debug.LogError("存在空系统挂载至EcsWorld的调用");
                return;
            }

            if (_systems.Contains(system)) return;
            
            system.Initialize();
            var index = 0;
            while (index < _systems.Count() &&
                   _systems[index].UpdateOrder < system.UpdateOrder)
                index++;
            _systems.Insert(index, system);
            _sortedUpdateDelegates.Insert(index, system.Update);
        }

        public void RemoveEcsSystem(IEcsSystem system)
        {
            int index = _systems.IndexOf(system);
            if (index != -1)
            {
                _systems.RemoveAt(index);
                _sortedUpdateDelegates.RemoveAt(index);
                system.Destroy();
            }
        }
        // 游戏启动时调用
        private void Initialize()
        {
            EcsManager = new EcsEntityManager();
            // 初始化其他ECS系统
            AddEcsSystem(new EntitySpawnSystem());
        }

        protected void Update()
        {
            foreach (var action in _sortedUpdateDelegates)
            {
                action.Invoke();
            }
        }

        #region EcsManager
        // 简化全局组件获取方法
        public static T GetComponent<T>(EcsEntity entity) where T : struct, IEcsComponent
        {
            return Instance.EcsManager.GetComponent<T>(entity);
        }

        public static void AddComponent<T>(EcsEntity entity, T component) where T : struct, IEcsComponent
        {
            Instance.EcsManager.AddComponent(entity, component);
        }

        public static void SetComponent<T>(EcsEntity entity, T component) where T : struct, IEcsComponent
        {
            Instance.EcsManager.SetComponent(entity, component);
        }

        public static bool HasComponent<T>(EcsEntity entity) where T : struct, IEcsComponent
        {
            return Instance.EcsManager.HasComponent<T>(entity);
        }

        public static void RemoveComponent<T>(EcsEntity entity) where T : struct, IEcsComponent
        {
            Instance.EcsManager.RemoveComponent<T>(entity);
        }

        public static bool Exists(EcsEntity entity)
        {
            return Instance.EcsManager.Exists(entity);
        }
        #endregion
    }
}
