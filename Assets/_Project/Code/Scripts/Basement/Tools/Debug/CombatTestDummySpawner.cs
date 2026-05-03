using System.Collections;
using Core.ECS;
using UnityEngine;

namespace Core.Entity
{
    /// <summary>
    /// 测试场景专用：额外生成一个局内单位（木桩 / 敌对目标），走与 <see cref="TestPlayerSpawner"/> 相同的 ECS 入队流程，
    /// 但不创建 DetailStatement（无需 <see cref="Core.UI.UIManager"/>）。
    /// Prefab 根节点需 <see cref="EntityBase"/> + <see cref="CombatEntitySpawnProfile"/>；
    /// 与主攻方敌对阵营（如主攻 <see cref="FactionTeamId.Blue"/> 则木桩用 <see cref="FactionTeamId.Red"/> 或 <see cref="FactionTeamId.Neutral"/>）。
    /// </summary>
    public sealed class CombatTestDummySpawner : MonoBehaviour
    {
        [SerializeField] private EntityBase dummyPrefab;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private bool spawnOnStart = true;

        private void Start()
        {
            if (spawnOnStart)
                StartCoroutine(SpawnRoutine());
        }

        /// <summary>可在其它调试脚本或 Inspector 按钮中调用。</summary>
        public void SpawnDummy()
        {
            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            yield return null;

            if (dummyPrefab == null)
            {
                Debug.LogError("[CombatTestDummySpawner] dummyPrefab 未赋值。");
                yield break;
            }

            var spawnSystem = EcsWorld.Instance?.GetEcsSystem<EntitySpawnSystem>();
            if (spawnSystem == null)
            {
                Debug.LogError("[CombatTestDummySpawner] EntitySpawnSystem 未注册。");
                yield break;
            }

            var parent = spawnParent != null ? spawnParent : transform;
            var instance = Instantiate(dummyPrefab.gameObject, parent.position + localOffset, parent.rotation, parent)
                .GetComponent<EntityBase>();

            if (instance == null)
            {
                Debug.LogError("[CombatTestDummySpawner] Prefab 根对象需带 EntityBase。");
                yield break;
            }

            spawnSystem.AddPendingEntity(instance);
            spawnSystem.FlushPendingEntitiesNow();

            if (instance.entityBridge == null || !instance.entityBridge.IsValid())
            {
                Debug.LogError(
                    "[CombatTestDummySpawner] ECS 绑定失败：entityBridge 无效。检查 Prefab 是否含 EcsEntityBridge、EcsWorld 与首个实体 Id。");
                yield break;
            }

            Debug.Log($"[CombatTestDummySpawner] 木桩已入队并完成 ECS 绑定: {instance.gameObject.name}, ecsId={instance.BoundEcsEntity.Id}");
        }
    }
}
