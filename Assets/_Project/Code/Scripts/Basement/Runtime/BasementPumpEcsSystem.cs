using Core.ECS;

namespace Basement.Runtime
{
    /// <summary>
    /// 空壳 ECS 系统：在 <see cref="EcsWorld"/> 的 <c>Update</c> 中推进 Basement 调度，减少对场景中 Dispatcher 的依赖。
    /// </summary>
    public sealed class BasementPumpEcsSystem : IEcsSystem
    {
        public int UpdateOrder => 0;

        public void Initialize()
        {
        }

        public void Update()
        {
            BasementUnityPump.PumpUpdate();
        }

        public void Destroy()
        {
        }
    }
}
