using Core.Entity;
using Core.ECS;

namespace Gameplay.Combat.Targeting
{
    /// <summary>最近敌对 + 校验：供 <see cref="DefaultTargetAcquisitionService"/>、<see cref="HostileAcquisitionCombatBoardAlign"/> 共用。</summary>
    public static class HostileTargetPicker
    {
        public static bool TryPickNearestValidatedHostile(
            EntityBase caster,
            float rangeOrRadius,
            bool includeDead,
            out EntityBase hostile,
            out string error)
        {
            hostile = null;
            error = null;

            if (caster == null)
            {
                error = "caster is null";
                return false;
            }

            var casterEcs = caster.BoundEcsEntity;
            if (!casterEcs.IsValid())
            {
                error = "invalid caster ecs";
                return false;
            }

            float range = CombatTargetingRange.ResolveForCaster(caster, rangeOrRadius);
            if (range <= 1e-6f)
            {
                error = "resolved range is 0";
                return false;
            }

            if (!casterEcs.HasComponent<FactionComponent>())
            {
                error = "caster has no FactionComponent";
                return false;
            }

            var myFaction = casterEcs.GetComponent<FactionComponent>().TeamId;

            if (!CombatTargetAcquire.TryPickNearestHostileInRange(casterEcs, myFaction, range, out var picked))
            {
                error = "no hostile in range";
                return false;
            }

            if (!EntityEcsLinkRegistry.TryGetEntityBase(picked, out hostile))
            {
                error = "picked has no EntityBase in registry";
                return false;
            }

            if (!MeleeStrikeRules.TryValidateMeleeStrike(caster, hostile, range, includeDead, out error))
                return false;

            return true;
        }
    }
}
