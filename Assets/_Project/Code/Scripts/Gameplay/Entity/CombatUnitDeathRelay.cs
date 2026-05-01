using Basement.Events;
using Core.ECS;
using Core.Entity;
using UnityEngine;

namespace Gameplay.Entity
{
    /// <summary>
    /// 毕设 Demo：击倒一次播报 — <see cref="Debug.Log"/> + <see cref="GameEventBus"/> + 既有 <see cref="UnitDeathEventHub"/>。<br/>
    /// 宿主禁用/销毁、助攻、金币等仍在后续接入。
    /// </summary>
    public static class CombatUnitDeathRelay
    {
        public static void AnnounceFirstDeath(EcsEntity victim, long killerEntityId)
        {
            if (!victim.IsValid())
                return;

            Debug.Log($"[Combat][Death] victimEcsId={victim.Id} killerEcsId={killerEntityId}");

            UnitDeathEventHub.Raise(victim, killerEntityId);

            GameEventBus.Instance.Initialize();
            GameEventBus.Instance.Publish(new CombatUnitDiedGameEvent
            {
                VictimEntityId = victim.Id,
                KillerEntityId = killerEntityId
            });
        }
    }
}
