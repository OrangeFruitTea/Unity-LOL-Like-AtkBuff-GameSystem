using Core.Entity;
using Core.ECS;
using UnityEngine;

namespace Core.Entity.Minions
{
    /// <summary>
    /// 兵线 Prefab：注册后挂载 <see cref="LaneMinionModuleComponent"/>。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LaneMinionEcsAttachments : MonoBehaviour, IEntitySpawnExtension
    {
        [SerializeField] private int waveSpawnId;
        [SerializeField] private byte laneIndex;
        [SerializeField] private int pathwayId;

        public void OnAfterEcsBaseSpawned(EcsEntity ecs, EntityBase host)
        {
            var lane = new LaneMinionModuleComponent();
            lane.InitializeDefaults();
            lane.WaveSpawnId = waveSpawnId;
            lane.LaneIndex = laneIndex;
            lane.PathwayId = pathwayId;
            lane.WaypointIndex = 0;
            EcsWorld.AddComponent(ecs, lane);
        }
    }
}
