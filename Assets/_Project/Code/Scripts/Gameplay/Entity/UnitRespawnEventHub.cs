using System;
using Core.ECS;

namespace Core.Entity
{
    /// <summary>
    /// 与 <see cref="UnitDeathEventHub"/> 对称：宿主完成 ECS 回血与传送后的单次钩子。
    /// </summary>
    public static class UnitRespawnEventHub
    {
        public static event Action<EcsEntity> UnitRespawned;

        public static void Raise(EcsEntity unit)
        {
            UnitRespawned?.Invoke(unit);
        }
    }
}
