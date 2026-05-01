using Core.Entity;

namespace Gameplay.Combat.Targeting
{
    /// <summary>
    /// 复用 <see cref="HostileTargetPicker"/> / <see cref="CombatBoardTargetSync"/>，
    /// 与塔 IdleScan→黑板 语义一致（设计文档 §2.7）。
    /// </summary>
    public static class HostileAcquisitionCombatBoardAlign
    {
        public static bool TryAcquireNearestHostileAndAlignCombatBoard(
            EntityBase caster,
            float rangeOrRadius,
            out EntityBase hostile,
            out string error)
        {
            hostile = null;
            error = null;

            if (!HostileTargetPicker.TryPickNearestValidatedHostile(
                    caster,
                    rangeOrRadius,
                    includeDead: false,
                    out hostile,
                    out error))
                return false;

            if (!CombatBoardTargetSync.SetAttackAndThreatSameTarget(caster, hostile.BoundEcsEntity.Id))
            {
                error = $"caster lacks {nameof(CombatBoardLiteComponent)}";
                return false;
            }

            return true;
        }
    }
}
