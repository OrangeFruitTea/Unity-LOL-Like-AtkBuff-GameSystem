using UnityEngine;

namespace Core.Entity.Minions
{
    /// <summary>
    /// 兵线共享路径（毕设级）；通常由 <see cref="Core.Entity.Spawn.MinionWaveSpawner"/> 在生成前写入。
    /// </summary>
    public static class LaneMinionWaypointRuntime
    {
        public static Transform[] Waypoints = System.Array.Empty<Transform>();

        public static void SetWaypoints(Transform[] waypoints)
        {
            Waypoints = waypoints ?? System.Array.Empty<Transform>();
        }
    }
}
