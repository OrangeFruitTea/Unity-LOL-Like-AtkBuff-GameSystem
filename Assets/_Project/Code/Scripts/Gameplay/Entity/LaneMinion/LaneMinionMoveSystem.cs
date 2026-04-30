using Core.ECS;

namespace Core.Entity.Minions
{
    /// <summary> 兵线 waypoint / Nav 推进；黑板仅承载目标 §6、§8.3。毕设可先留空。</summary>
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
        }
    }
}
