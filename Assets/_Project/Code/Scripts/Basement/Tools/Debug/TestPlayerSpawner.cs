using System.Collections;
using Core.ECS;
using Core.UI;
using UnityEngine;

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
        [SerializeField] private EntityBase testPlayerPrefab;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private Vector3 localOffset;

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
            var instance = Instantiate(testPlayerPrefab.gameObject, parent.position + localOffset, parent.rotation, parent)
                .GetComponent<EntityBase>();

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

            var ui = UIManager.Instance;
            if (ui == null)
            {
                Debug.LogError("[TestPlayerSpawner] UIManager.Instance 为空，无法生成 DetailStatement。");
                yield break;
            }

            if (!ui.TrySpawnDetailStatement(out var hudId, out var root))
                yield break;

            Debug.Log($"[TestPlayerSpawner] DetailStatement 已生成 instanceId={hudId}, root={root.name}");
            ui.BindEcsBridgeConsumers(root, instance.entityBridge);
        }
    }
}
