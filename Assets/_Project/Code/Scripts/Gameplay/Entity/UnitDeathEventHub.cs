using System;
using Core.ECS;

namespace Core.Entity
{
    /// <summary>
    /// MVP+：击倒后由 <see cref="UnitVitalitySystem"/> 触发一次的全局钩子；宿主销毁应由监听方对场景对象处理（勿在此直接 Destroy ECS）。
    /// </summary>
    public static class UnitDeathEventHub
    {
        public static event Action<EcsEntity, long> UnitDied;

        public static void Raise(EcsEntity victim, long killerEntityId)
        {
            UnitDied?.Invoke(victim, killerEntityId);
        }
    }
}
