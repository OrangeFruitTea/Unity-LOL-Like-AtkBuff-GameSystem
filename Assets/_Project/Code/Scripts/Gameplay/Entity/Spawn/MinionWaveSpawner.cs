using System.Collections;
using Basement.Tools;
using Core.Entity.Minions;
using Core.ECS;
using UnityEngine;

namespace Core.Entity.Spawn
{
    /// <summary>
    /// P1：按间隔生成一波兵线；路径点由 <see cref="waypointRoot"/> 子节点顺序提供，写入 <see cref="LaneMinionWaypointRuntime"/>。
    /// </summary>
    public sealed class MinionWaveSpawner : MonoBehaviour
    {
        [SerializeField] private EntityBase minionPrefab;
        [SerializeField] private Transform waypointRoot;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private float waveIntervalSeconds = 8f;
        [SerializeField] private int minionsPerWave = 3;
        [SerializeField] private float startDelaySeconds;

        private void Awake()
        {
            if (waypointRoot == null)
            {
                Debug.LogWarning($"{nameof(MinionWaveSpawner)}: 未指定 waypointRoot，兵线不会移动");
                LaneMinionWaypointRuntime.SetWaypoints(System.Array.Empty<Transform>());
                return;
            }

            var pts = new Transform[waypointRoot.childCount];
            for (int i = 0; i < waypointRoot.childCount; i++)
                pts[i] = waypointRoot.GetChild(i);
            LaneMinionWaypointRuntime.SetWaypoints(pts);
        }

        private void Start()
        {
            StartCoroutine(SpawnWaves());
        }

        private IEnumerator SpawnWaves()
        {
            if (startDelaySeconds > 0f)
                yield return new WaitForSeconds(startDelaySeconds);

            var spawnSystem = EcsWorld.Instance.GetEcsSystem<EntitySpawnSystem>();
            if (spawnSystem == null)
            {
                Debug.LogError($"{nameof(MinionWaveSpawner)}: EntitySpawnSystem 未注册");
                yield break;
            }

            if (minionPrefab == null)
            {
                Debug.LogError($"{nameof(MinionWaveSpawner)}: 未指定兵线 Prefab");
                yield break;
            }

            var parent = spawnParent != null ? spawnParent : transform;

            while (true)
            {
                for (int i = 0; i < minionsPerWave; i++)
                {
                    var spawned = Instantiate(minionPrefab.gameObject, parent.position + localOffset, parent.rotation);
                    TransformPlacementUtility.SetParentKeepWorldTransform(spawned.transform, parent);
                    var instance = spawned.GetComponent<EntityBase>();
                    if (instance == null)
                    {
                        Debug.LogError("兵线 Prefab 根节点需带 EntityBase");
                        yield break;
                    }

                    spawnSystem.AddPendingEntity(instance);
                }

                if (waveIntervalSeconds <= 0f)
                    yield break;

                yield return new WaitForSeconds(waveIntervalSeconds);
            }
        }
    }
}
