using Core.Entity;
using Core.ECS;

namespace Gameplay.Combat.Targeting
{
    /// <summary>近战普攻：敌对、存活、距离等共用校验。</summary>
    public static class MeleeStrikeRules
    {
        private const float HpEpsilon = 1e-6f;

        /// <summary>
        /// <paramref name="maxMeleeRangeOrZero"/> ≤0 则用攻击者 <see cref="EntityBaseData.AtkDistance"/>。
        /// </summary>
        public static bool TryValidateMeleeStrike(
            EntityBase attacker,
            EntityBase victim,
            float maxMeleeRangeOrZero,
            bool allowDead,
            out string error)
        {
            error = null;
            if (attacker == null || victim == null)
            {
                error = "attacker or victim is null";
                return false;
            }

            var srcEcs = attacker.BoundEcsEntity;
            var tgtEcs = victim.BoundEcsEntity;
            if (!srcEcs.IsValid() || !tgtEcs.IsValid())
            {
                error = "invalid ecs on attacker or victim";
                return false;
            }

            if (srcEcs.Id == tgtEcs.Id)
            {
                error = "cannot attack self";
                return false;
            }

            if (!srcEcs.HasComponent<FactionComponent>() || !tgtEcs.HasComponent<FactionComponent>())
            {
                error = "faction missing";
                return false;
            }

            if (!CombatHostility.AreHostile(
                    srcEcs.GetComponent<FactionComponent>().TeamId,
                    tgtEcs.GetComponent<FactionComponent>().TeamId))
            {
                error = "not hostile";
                return false;
            }

            if (!tgtEcs.HasComponent<EntityDataComponent>())
            {
                error = "victim missing EntityData";
                return false;
            }

            if (!allowDead)
            {
                if (tgtEcs.GetComponent<EntityDataComponent>().GetData(EntityBaseDataCore.CrtHp) <= HpEpsilon)
                {
                    error = "victim dead";
                    return false;
                }
            }

            if (!srcEcs.HasComponent<EntityDataComponent>())
            {
                error = "attacker missing EntityData";
                return false;
            }

            float maxDist = maxMeleeRangeOrZero > 1e-6f
                ? maxMeleeRangeOrZero
                : (float)srcEcs.GetComponent<EntityDataComponent>().GetData(EntityBaseData.AtkDistance);

            if (maxDist > 1e-6f &&
                EntityEcsLinkRegistry.TryGetEntityBase(srcEcs, out var ego) &&
                EntityEcsLinkRegistry.TryGetEntityBase(tgtEcs, out var other))
            {
                if ((ego.transform.position - other.transform.position).sqrMagnitude > maxDist * maxDist)
                {
                    error = "target out of range";
                    return false;
                }
            }

            return true;
        }
    }
}
