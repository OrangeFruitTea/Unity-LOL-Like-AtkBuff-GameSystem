using Basement.Tools;
using Core.ECS;
using UnityEngine;

namespace Core.Entity.Spawn
{
    /// <summary>
    /// P1：在营地位置生成一只野怪；专精与租赁圆心由 Prefab 上 <see cref="JungleCreepEcsAttachments"/> 处理。
    /// </summary>
    public sealed class JungleCampSpawner : MonoBehaviour
    {
        [SerializeField] private EntityBase creepPrefab;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private Vector3 localOffset;

        private void Start()
        {
            if (creepPrefab == null)
            {
                Debug.LogError($"{nameof(JungleCampSpawner)}: 未指定野怪 Prefab");
                return;
            }

            var spawnSystem = EcsWorld.Instance.GetEcsSystem<EntitySpawnSystem>();
            if (spawnSystem == null)
            {
                Debug.LogError($"{nameof(JungleCampSpawner)}: EntitySpawnSystem 未注册");
                return;
            }

            var parent = spawnParent != null ? spawnParent : transform;
            var spawned = Instantiate(creepPrefab.gameObject, parent.position + localOffset, parent.rotation);
            TransformPlacementUtility.SetParentKeepWorldTransform(spawned.transform, parent);
            var instance = spawned.GetComponent<EntityBase>();

            if (instance == null)
            {
                Debug.LogError("野怪 Prefab 根节点需带 EntityBase");
                return;
            }

            spawnSystem.AddPendingEntity(instance);
        }
    }
}
