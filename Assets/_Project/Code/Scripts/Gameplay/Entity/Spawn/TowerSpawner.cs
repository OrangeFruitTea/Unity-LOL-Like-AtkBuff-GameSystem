using Core.ECS;
using UnityEngine;

namespace Core.Entity.Spawn
{
    /// <summary>
    /// P1：实例化带 <see cref="EntityBase"/> 的塔 Prefab 并入队；专精组件由 Prefab 上 <see cref="TowerEcsAttachments"/> 在注册后附加。
    /// </summary>
    public sealed class TowerSpawner : MonoBehaviour
    {
        [SerializeField] private EntityBase towerPrefab;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private Vector3 localOffset;

        private void Start()
        {
            if (towerPrefab == null)
            {
                Debug.LogError($"{nameof(TowerSpawner)}: 未指定塔 Prefab");
                return;
            }

            var spawnSystem = EcsWorld.Instance.GetEcsSystem<EntitySpawnSystem>();
            if (spawnSystem == null)
            {
                Debug.LogError($"{nameof(TowerSpawner)}: EntitySpawnSystem 未注册");
                return;
            }

            var parent = spawnParent != null ? spawnParent : transform;
            var instance = Instantiate(towerPrefab.gameObject, parent.position + localOffset, parent.rotation, parent)
                .GetComponent<EntityBase>();

            if (instance == null)
            {
                Debug.LogError("塔 Prefab 根节点需带 EntityBase");
                return;
            }

            spawnSystem.AddPendingEntity(instance);
        }
    }
}
