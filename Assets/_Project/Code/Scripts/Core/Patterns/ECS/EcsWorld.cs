using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Entity;
using Core.Combat;
using Core.Entity.Jungle;
using Core.Entity.Minions;
using UnityEngine;
using Core.Gameplay;
using Basement.Runtime;
using Basement.Utils;
using Basement.Json;
using Basement.Logging;
using Gameplay.Skill.Config;
using Gameplay.Skill.Loading;

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

        private ImpactManager _combatImpacts;

        /// <summary>
        /// 全工程唯一 <see cref="ImpactManager"/>；<b>首次访问</b>时创建。<br/>
        /// 不要求与 <see cref="AddEcsSystem"/> 在源码行上相邻；各系统在 <see cref="IEcsSystem.Initialize"/> 里取用即可。
        /// </summary>
        public ImpactManager CombatImpacts
        {
            get
            {
                if (_combatImpacts == null)
                    _combatImpacts = new ImpactManager(this);
                return _combatImpacts;
            }
        }

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
            _combatImpacts = null;
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
            WarmStreamingGameTablesEarly();
            EcsManager = new EcsEntityManager();
            AddEcsSystem(new EntitySpawnSystem());
            AddEcsSystem(new TowerCombatCycleSystem());
            AddEcsSystem(new ImpactSystem());
            AddEcsSystem(new UnitVitalitySystem());
            AddEcsSystem(new JungleAiSystem());
            AddEcsSystem(new LaneMinionMoveSystem());
        }

        protected void Update()
        {
            foreach (var action in _sortedUpdateDelegates)
            {
                action.Invoke();
            }
        }

        protected void FixedUpdate()
        {
            BasementUnityPump.PumpFixedUpdate();
        }

        /// <summary>
        /// World 就绪最前一刻：拉起 Json/Buff 宿主并灌入技能目录；场景里的 <see cref="SkillDataLoader"/> 仍可再次 <c>Load()</c> 覆盖。
        /// </summary>
        private static bool _streamingGameTablesWarmOnce;

        private static void WarmStreamingGameTablesEarly()
        {
            _ = JsonManager.Instance;
            _ = LogManager.Instance;

            if (BuffDataLoader.Instance == null)
            {
                var go = new GameObject("[EcsWorld]BuffDataLoader");
                go.AddComponent<BuffDataLoader>();
            }

            if (_streamingGameTablesWarmOnce)
                return;
            _streamingGameTablesWarmOnce = true;

            if (!TryReloadSkillCatalogFromStreaming(SkillDataLoader.DefaultRelativePath, out var err) &&
                !string.IsNullOrEmpty(err))
                Debug.LogWarning($"[EcsWorld] {err}");
        }

        /// <summary>
        /// 等价于 StreamingAssets JSON → <see cref="SkillCatalog"/>；供 <see cref="SkillDataLoader"/> 运行时重载共用。
        /// </summary>
        public static bool TryReloadSkillCatalogFromStreaming(string relativePath, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(relativePath))
                relativePath = SkillDataLoader.DefaultRelativePath;

            string full = Path.Combine(Application.streamingAssetsPath, relativePath);
            if (string.IsNullOrEmpty(full))
            {
                error = "invalid path";
                SkillCatalog.ReplaceAll(Array.Empty<SkillDefinition>());
                return false;
            }

            if (!File.Exists(full))
            {
                error = $"技能表不存在: {full}，SkillCatalog 置空。";
                Debug.LogWarning($"[EcsWorld] {error}");
                SkillCatalog.ReplaceAll(Array.Empty<SkillDefinition>());
                return false;
            }

            _ = JsonManager.Instance;
            if (JsonManager.Instance == null)
            {
                error = "JsonManager.Instance 不可用";
                Debug.LogError($"[EcsWorld] {error}");
                return false;
            }

            var result = JsonManager.Instance.DeserializeFromFilePath<SkillDataFileDto>(
                full,
                JsonSerializerProfile.GameContent);
            if (!result.Success)
            {
                error = result.Error ?? "Deserialize failed";
                Debug.LogError($"[EcsWorld] 技能表解析失败: {error}");
                return false;
            }

            var dto = result.Value;
            SkillCatalog.ReplaceAll(dto.Skills ?? Array.Empty<SkillDefinition>());
            Debug.Log($"[EcsWorld] 已加载技能 {dto.Skills?.Count ?? 0} 条 (schema {dto.SchemaVersion})");
            return true;
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

        public EcsEntity CreateEntity() => EcsManager.CreateEntity();

        public void DestroyEntity(EcsEntity entity) => EcsManager.DestroyEntity(entity);

        public IEnumerable<EcsEntity> GetEntitiesWithComponent<T>() where T : struct, IEcsComponent
            => EcsManager.GetEntitiesWithComponent<T>();

        public IEnumerable<EcsEntity> GetEntitiesWithAllComponents(params Type[] componentTypes)
            => EcsManager.GetEntitiesWithAll(componentTypes);
        #endregion
    }
}
