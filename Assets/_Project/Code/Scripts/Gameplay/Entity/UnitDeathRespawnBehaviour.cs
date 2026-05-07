using System.Collections;
using Core.ECS;
using Core.Entity.Jungle;
using Core.Entity.Minions;
using Gameplay.Entity;
using Gameplay.Presentation;
using UnityEngine;
using UnityEngine.AI;

namespace Core.Entity
{
    /// <summary>
    /// 方案 A：英雄倒地不销毁，延时复活；方案 C：野怪 / 小兵同款软复活并传送回 <see cref="UnitSpawnAnchor"/>。<br/>
    /// 需关闭同物体上 <see cref="DestroyHostOnUnitDeath"/> 的销毁路径（或依赖本组件的 <see cref="SuppressHostDestroy"/> 让销毁脚本跳过）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnitDeathRespawnBehaviour : MonoBehaviour
    {
        /// <summary>配置默认值：英雄较长延时；野怪较短。</summary>
        public enum RespawnKind
        {
            Hero = 0,
            CreepOrMinion = 1
        }

        [SerializeField]
        private EntityBase entity;

        [SerializeField]
        private UnitSpawnAnchor spawnAnchor;

        [SerializeField]
        private NavMeshAgent navMeshAgent;

        [SerializeField]
        private UnitAnimDrv animDriver;

        [Tooltip("英雄建议 3～8s；野怪可更短。")]
        [SerializeField]
        private RespawnKind kind = RespawnKind.Hero;

        [SerializeField]
        private float heroRespawnDelaySeconds = 5f;

        [SerializeField]
        private float creepRespawnDelaySeconds = 3f;

        [Tooltip("复活后是否回满魔法。")]
        [SerializeField]
        private bool refillMpOnRespawn = true;

        [Tooltip("为 true 时同物体上的 DestroyHostOnUnitDeath 不会销毁或隐藏宿主。")]
        [SerializeField]
        private bool suppressHostDestroy = true;

        [Tooltip("倒地期间禁用 NavMeshAgent（存在时）。")]
        [SerializeField]
        private bool disableNavAgentWhileDead = true;

        [Tooltip("倒地期间禁用 MovementController（玩家点击移动）。")]
        [SerializeField]
        private bool disableMovementControllerWhileDead = true;

        [SerializeField]
        private float navMeshSampleRadius = 4f;

        private MovementController _movement;
        private Coroutine _respawnCo;

        /// <summary>供 <see cref="DestroyHostOnUnitDeath"/> 检测。</summary>
        public bool SuppressHostDestroy => suppressHostDestroy && enabled;

        private void Awake()
        {
            if (entity == null)
                entity = GetComponent<EntityBase>();
            if (spawnAnchor == null)
                spawnAnchor = GetComponent<UnitSpawnAnchor>();
            if (spawnAnchor == null)
                spawnAnchor = gameObject.AddComponent<UnitSpawnAnchor>();
            if (navMeshAgent == null)
                navMeshAgent = GetComponent<NavMeshAgent>();
            if (animDriver == null)
                animDriver = GetComponent<UnitAnimDrv>();
            _movement = GetComponent<MovementController>();
        }

        private void OnEnable()
        {
            UnitDeathEventHub.UnitDied += OnUnitDied;
        }

        private void OnDisable()
        {
            UnitDeathEventHub.UnitDied -= OnUnitDied;
            if (_respawnCo != null)
            {
                StopCoroutine(_respawnCo);
                _respawnCo = null;
            }
        }

        private void OnUnitDied(EcsEntity victim, long killerEntityId)
        {
            _ = killerEntityId;
            if (!isActiveAndEnabled || entity == null)
                return;
            if (!victim.IsValid() || victim.Id != entity.BoundEcsEntity.Id)
                return;

            if (_respawnCo != null)
                return;

            ApplyDeadPresentationConstraints(true);

            float delay = kind == RespawnKind.Hero ? heroRespawnDelaySeconds : creepRespawnDelaySeconds;
            _respawnCo = StartCoroutine(RespawnAfterDelay(delay));
        }

        private IEnumerator RespawnAfterDelay(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            TryRespawnAtSpawn();
            _respawnCo = null;
        }

        private void TryRespawnAtSpawn()
        {
            if (entity == null || !entity.BoundEcsEntity.IsValid())
                return;

            var ecs = entity.BoundEcsEntity;
            if (!ecs.HasComponent<EntityDataComponent>())
                return;

            Vector3 rawSpawn = spawnAnchor != null ? spawnAnchor.SpawnWorldPosition : transform.position;
            Quaternion spawnRot = spawnAnchor != null ? spawnAnchor.SpawnWorldRotation : transform.rotation;

            if (!NavMesh.SamplePosition(rawSpawn, out var navHit, navMeshSampleRadius, NavMesh.AllAreas))
                navHit.position = rawSpawn;

            var data = ecs.GetComponent<EntityDataComponent>();
            double maxHp = data.GetData(EntityBaseDataCore.HpLimit);
            data.SetData(EntityBaseDataCore.CrtHp, maxHp);
            if (refillMpOnRespawn)
                data.SetData(EntityBaseDataCore.CrtMp, data.GetData(EntityBaseDataCore.MpLimit));
            ecs.SetComponent(data);

            if (ecs.HasComponent<CombatBoardLiteComponent>())
            {
                var board = ecs.GetComponent<CombatBoardLiteComponent>();
                board.InitializeDefaults();
                ecs.SetComponent(board);
            }

            if (ecs.HasComponent<JungleCreepModuleComponent>())
            {
                var module = ecs.GetComponent<JungleCreepModuleComponent>();
                module.CurrentState = JungleCreepState.Idle;
                ecs.SetComponent(module);
            }

            if (ecs.HasComponent<LaneMinionModuleComponent>())
            {
                var lane = ecs.GetComponent<LaneMinionModuleComponent>();
                lane.WaypointIndex = 0;
                ecs.SetComponent(lane);
            }

            ApplyDeadPresentationConstraints(false);

            transform.SetPositionAndRotation(navHit.position, spawnRot);
            if (navMeshAgent != null)
            {
                navMeshAgent.Warp(navHit.position);
                navMeshAgent.ResetPath();
            }

            animDriver?.NotifyRevived();

            UnitRespawnEventHub.Raise(ecs);

            GameEventBus.Instance.Initialize();
            GameEventBus.Instance.Publish(new CombatUnitRespawnedGameEvent { UnitEntityId = ecs.Id });
        }

        private void ApplyDeadPresentationConstraints(bool dead)
        {
            if (disableNavAgentWhileDead && navMeshAgent != null)
                navMeshAgent.enabled = !dead;

            if (disableMovementControllerWhileDead && _movement != null)
                _movement.enabled = !dead;
        }
    }
}
