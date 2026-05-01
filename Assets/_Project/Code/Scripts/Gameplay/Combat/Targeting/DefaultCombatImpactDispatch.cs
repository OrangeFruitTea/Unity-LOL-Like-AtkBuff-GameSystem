using Core.Combat;
using Core.Entity;
using Core.ECS;
using Core.Gameplay;

namespace Gameplay.Combat.Targeting
{
    public sealed class DefaultCombatImpactDispatch : ICombatImpactDispatch
    {
        public bool TryDispatchNormalAttack(EntityBase attacker, out string error)
        {
            error = null;

            if (attacker == null)
            {
                error = "attacker is null";
                return false;
            }

            if (!CombatBoardTargetSync.TryGetPrimaryAttackTarget(attacker, out var victim))
            {
                error = "no primary attack target on combat board";
                return false;
            }

            if (!MeleeStrikeRules.TryValidateMeleeStrike(
                    attacker,
                    victim,
                    maxMeleeRangeOrZero: 0f,
                    allowDead: false,
                    out error))
                return false;

            var impacts = EcsWorld.Instance.CombatImpacts;
            if (impacts == null)
            {
                error = "CombatImpacts not initialized on EcsWorld";
                return false;
            }

            double raw =
                attacker.BoundEcsEntity.GetComponent<EntityDataComponent>()
                    .GetData(EntityBaseDataCore.AtkAD);

            impacts.CreateImpactEvent(
                attacker.BoundEcsEntity,
                victim.BoundEcsEntity,
                TargetAttribute.Hp,
                (float)raw,
                ImpactOperationType.Subtract,
                ImpactType.Physical,
                ImpactSourceType.NormalAtk);

            return true;
        }
    }
}
