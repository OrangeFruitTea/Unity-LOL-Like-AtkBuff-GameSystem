using Core.ECS;
using UnityEngine;

namespace Core.Entity.Minions
{
    /// <summary> 兵线 waypoint 推进；路径由 <see cref="LaneMinionWaypointRuntime"/> 提供。 </summary>
    public sealed class LaneMinionMoveSystem : IEcsSystem
    {
        public int UpdateOrder => 39;

        public void Initialize()
        {
        }

        public void Destroy()
        {
        }

        public void Update()
        {
            var waypoints = LaneMinionWaypointRuntime.Waypoints;
            if (waypoints == null || waypoints.Length == 0)
                return;

            foreach (var ecs in EcsWorld.Instance.GetEntitiesWithComponent<LaneMinionModuleComponent>())
            {
                if (!ecs.HasComponent<EntityDataComponent>())
                    continue;

                var data = ecs.GetComponent<EntityDataComponent>();
                if (data.GetData(EntityBaseDataCore.CrtHp) <= 1e-9)
                    continue;

                if (!EntityEcsLinkRegistry.TryGetEntityBase(ecs, out var host))
                    continue;

                var lane = ecs.GetComponent<LaneMinionModuleComponent>();
                int idx = lane.WaypointIndex;
                if (idx >= waypoints.Length || waypoints[idx] == null)
                    continue;

                Vector3 target = waypoints[idx].position;
                Vector3 pos = host.transform.position;
                float speed = (float)data.GetData(EntityBaseDataCore.MoveSpeed);
                host.transform.position = Vector3.MoveTowards(pos, target, speed * Time.deltaTime);

                if ((host.transform.position - target).sqrMagnitude < 0.15f * 0.15f &&
                    lane.WaypointIndex < waypoints.Length - 1)
                {
                    lane.WaypointIndex++;
                    ecs.SetComponent(lane);
                }
            }
        }
    }
}
