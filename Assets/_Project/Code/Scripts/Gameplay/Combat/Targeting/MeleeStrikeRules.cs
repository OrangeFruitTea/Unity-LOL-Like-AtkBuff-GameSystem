using Core.Entity;
using Core.ECS;

namespace Gameplay.Combat.Targeting
{
    /// <summary>近战普攻：敌对、存活、距离等共用校验。</summary>
    public static class MeleeStrikeRules
    {
        private const float HpEpsilon = 1e-6f;

        /// <summary>
        /// <paramref name="maxMeleeRangeOrZero"/>≤0：从攻击者 <see cref="EntityBaseData.AtkDistance"/> 读取，再经 <see cref="CombatTargetingRange.StoredAttackDistanceToWorldDistance"/> 为世界距离。<br/>
        /// 为正：视作调用方传入的<strong>世界距离上限</strong>（索敌链路会先 <see cref="CombatTargetingRange.ResolveForCasterWorldDistance"/>）。
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
                : CombatTargetingRange.StoredAttackDistanceToWorldDistance(
                    (float)srcEcs.GetComponent<EntityDataComponent>().GetData(EntityBaseData.AtkDistance));

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
