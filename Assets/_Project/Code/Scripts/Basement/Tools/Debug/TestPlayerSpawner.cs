using System;
using System.Collections;
using Basement.Tools;
using Core.CameraSystem;
using Core.ECS;
using Core.UI;
using UnityEngine;
using Widgets.PlayerStatement;

namespace Core.Entity
{
    public class TestPlayer : EntityBase
    {
    }

    /// <summary>
    /// 从 Prefab <see cref="Instantiate"/> 得到场景实例再入队，避免把 Prefab 资产直接当队列项（无独立 Transform/Mono 生命周期）。
    /// 目标实例上需有 <see cref="CombatEntitySpawnProfile"/> 方能挂上阵营/黑板/原型（见 EntitySpawnSystem）。
    /// </summary>
    public class TestPlayerSpawner : EntityBase
    {
        /// <summary>最近一次 ECS 绑定成功的测试玩家根 <see cref="Transform"/>（用于相机等在 Inspector 未绑定时自动对齐）。</summary>
        public static Transform LastSpawnedPlayerRoot { get; private set; }

        /// <summary>在 <see cref="FlushPendingEntitiesNow"/> 且 <c>entityBridge</c> 有效后触发；早于 UI/HUD 绑定。</summary>
        public static event Action<Transform> TestPlayerSpawned;

        [SerializeField] private EntityBase testPlayerPrefab;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private Vector3 localOffset;

        [Tooltip(
            "若为 true：玩家 ECS 就绪后动态创建带 CameraController 的相机并设为 MainCamera；场景中旧 MainCamera 会被停用并取消 Tag（见 PlayerFollowCameraSpawner）。")]
        [SerializeField]
        private bool spawnDedicatedFollowCamera = true;

        private bool _isTestPlayerPrefabNull;

        private void Start()
        {
            _isTestPlayerPrefabNull = testPlayerPrefab == null;
            StartCoroutine(SpawnTestPlayer());
        }

        private IEnumerator SpawnTestPlayer()
        {
            yield return null;
            if (_isTestPlayerPrefabNull)
            {
                Debug.LogError("需要在Inspector中赋值TestPlayer预制体");
                yield break;
            }

            var spawnSystem = EcsWorld.Instance.GetEcsSystem<EntitySpawnSystem>();
            if (spawnSystem == null)
            {
                Debug.LogError("EntitySpawnSystem未注册");
                yield break;
            }

            var parent = spawnParent != null ? spawnParent : transform;
            var spawned = Instantiate(testPlayerPrefab.gameObject, parent.position + localOffset, parent.rotation);
            TransformPlacementUtility.SetParentKeepWorldTransform(spawned.transform, parent);
            var instance = spawned.GetComponent<EntityBase>();

            if (instance == null)
            {
                Debug.LogError("Prefab 根对象需带 EntityBase（或 TestPlayer）");
                yield break;
            }

            spawnSystem.AddPendingEntity(instance);
            Debug.Log($"TestPlayer 实例已加入生成队列: {instance.gameObject.name}");

            spawnSystem.FlushPendingEntitiesNow();

            if (instance.entityBridge == null || !instance.entityBridge.IsValid())
            {
                Debug.LogError(
                    "[TestPlayerSpawner] ECS 绑定失败：entityBridge 无效。检查 EcsWorld / EntitySpawnSystem，以及 EcsEntity.Id 是否为 0（首个实体 Id 不可为 0）。");
                yield break;
            }

            if (spawnDedicatedFollowCamera)
                PlayerFollowCameraSpawner.CreateForPlayer(instance.transform);

            LastSpawnedPlayerRoot = instance.transform;
            TestPlayerSpawned?.Invoke(instance.transform);

            var ui = UIManager.Instance;
            if (ui == null)
            {
                Debug.LogError("[TestPlayerSpawner] UIManager.Instance 为空，无法生成 StatementWidget。");
                yield break;
            }

            if (!ui.TrySpawnStatementWidget(out var hudId, out var root))
                yield break;

            Debug.Log($"[TestPlayerSpawner] StatementWidget 已生成 instanceId={hudId}, root={root.name}");

            var bridge = instance.entityBridge;
            var statementWidget = root.GetComponent<StatementWidget>()
                                 ?? root.GetComponentInChildren<StatementWidget>(true);
            if (statementWidget == null)
            {
                Debug.LogError(
                    $"[TestPlayerSpawner] 生成的 HUD 根节点上缺少 {nameof(StatementWidget)}，无法绑定玩家 ECS。（root={root.name}）");
                yield break;
            }

            statementWidget.Bind(bridge);
        }
    }
}
