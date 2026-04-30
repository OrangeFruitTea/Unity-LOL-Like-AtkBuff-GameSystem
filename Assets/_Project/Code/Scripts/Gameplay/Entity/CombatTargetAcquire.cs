using Core.ECS;
using UnityEngine;

namespace Core.Entity
{
    /// <summary>
    /// 索敌辅助：在 <paramref name="range"/> 内按距离选最近 **敌对** 单位（需 <see cref="FactionComponent"/> + <see cref="EntityDataComponent"/>）。
    /// 毕设实现：无空间索引，全表扫描；表现与权威一致需后续优化。
    /// </summary>
    public static class CombatTargetAcquire
    {
        public static bool TryPickNearestHostileInRange(
            EcsEntity aggressor,
            FactionTeamId aggressorFaction,
            float range,
            out EcsEntity target)
        {
            target = default;
            if (!aggressor.IsValid() || range <= 0f)
                return false;

            float radiusSqr = range * range;
            float bestSqr = float.MaxValue;
            EcsEntity best = default;

            foreach (var candidate in EcsWorld.Instance.GetEntitiesWithComponent<EntityDataComponent>())
            {
                if (candidate.Id == aggressor.Id)
                    continue;
                if (!candidate.HasComponent<FactionComponent>())
                    continue;

                var otherFaction = candidate.GetComponent<FactionComponent>().TeamId;
                if (!CombatHostility.AreHostile(aggressorFaction, otherFaction))
                    continue;

                if (!EntityEcsLinkRegistry.TryGetEntityBase(aggressor, out var ego) ||
                    !EntityEcsLinkRegistry.TryGetEntityBase(candidate, out var other))
                    continue;

                float sqr = (ego.transform.position - other.transform.position).sqrMagnitude;
                if (sqr > radiusSqr)
                    continue;
                if (!(sqr < bestSqr))
                    continue;

                bestSqr = sqr;
                best = candidate;
            }

            if (bestSqr >= float.MaxValue)
                return false;

            target = best;
            return true;
        }

        /// <summary>
        /// 以世界坐标 <paramref name="origin"/> 为球心（用于野区租赁圆心等），在 <paramref name="range"/> 内选最近敌对单位。
        /// </summary>
        public static bool TryPickNearestHostileInRangeFromWorldPoint(
            UnityEngine.Vector3 origin,
            EcsEntity aggressor,
            FactionTeamId aggressorFaction,
            float range,
            out EcsEntity target)
        {
            target = default;
            if (!aggressor.IsValid() || range <= 0f)
                return false;

            float radiusSqr = range * range;
            float bestSqr = float.MaxValue;
            EcsEntity best = default;

            foreach (var candidate in EcsWorld.Instance.GetEntitiesWithComponent<EntityDataComponent>())
            {
                if (candidate.Id == aggressor.Id)
                    continue;
                if (!candidate.HasComponent<FactionComponent>())
                    continue;

                var otherFaction = candidate.GetComponent<FactionComponent>().TeamId;
                if (!CombatHostility.AreHostile(aggressorFaction, otherFaction))
                    continue;

                if (!EntityEcsLinkRegistry.TryGetEntityBase(candidate, out var other))
                    continue;

                float sqr = (origin - other.transform.position).sqrMagnitude;
                if (sqr > radiusSqr)
                    continue;
                if (!(sqr < bestSqr))
                    continue;

                bestSqr = sqr;
                best = candidate;
            }

            if (bestSqr >= float.MaxValue)
                return false;

            target = best;
            return true;
        }
    }
}
