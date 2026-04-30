using Core.Combat;
using Core.Entity;
using Core.ECS;
using Core.Gameplay;

namespace Gameplay.Combat.Targeting
{
    public sealed class DefaultCombatImpactDispatch : ICombatImpactDispatch
    {
        private const float HpEpsilon = 1e-6f;

        public bool TryDispatchNormalAttack(EntityBase attacker, EntityBase victim, out string error)
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

            if (tgtEcs.GetComponent<EntityDataComponent>().GetData(EntityBaseDataCore.CrtHp) <= HpEpsilon)
            {
                error = "victim dead";
                return false;
            }

            if (!srcEcs.HasComponent<EntityDataComponent>())
            {
                error = "attacker missing EntityData";
                return false;
            }

            float atkRange =
                (float)srcEcs.GetComponent<EntityDataComponent>().GetData(EntityBaseData.AtkDistance);

            if (atkRange > 1e-6f &&
                EntityEcsLinkRegistry.TryGetEntityBase(srcEcs, out var ego) &&
                EntityEcsLinkRegistry.TryGetEntityBase(tgtEcs, out var other))
            {
                if ((ego.transform.position - other.transform.position).sqrMagnitude > atkRange * atkRange)
                {
                    error = "out of attack range";
                    return false;
                }
            }

            double raw =
                srcEcs.GetComponent<EntityDataComponent>().GetData(EntityBaseDataCore.AtkAD);

            var impacts = new ImpactManager(EcsWorld.Instance);
            impacts.CreateImpactEvent(
                srcEcs,
                tgtEcs,
                TargetAttribute.Hp,
                (float)raw,
                ImpactOperationType.Subtract,
                ImpactType.Physical,
                ImpactSourceType.NormalAtk);

            return true;
        }
    }
}
