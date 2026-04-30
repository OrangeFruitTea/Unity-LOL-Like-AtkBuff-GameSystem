using Core.ECS;

namespace Core.Entity.Minions
{
    /// <summary> 兵线：波次 + 路径点；数值仍走 <see cref="Core.Entity.EntityDataComponent"/>。设计文档 §6。 </summary>
    public struct LaneMinionModuleComponent : IEcsComponent
    {
        public int WaveSpawnId;

        /// <summary> 上/中/下等路与关卡约定一致即可。 </summary>
        public byte LaneIndex;

        /// <summary> 指向 waypoint 列表或其它路径资源的主键。 </summary>
        public int PathwayId;

        /// <summary> 当前 waypoint 下标。 </summary>
        public ushort WaypointIndex;

        /// <summary> 可选：最近一次重寻路动机。 </summary>
        public LaneMinionRepathReason RepathReason;

        public void InitializeDefaults()
        {
            WaveSpawnId = 0;
            LaneIndex = 0;
            PathwayId = 0;
            WaypointIndex = 0;
            RepathReason = LaneMinionRepathReason.None;
        }
    }
}
