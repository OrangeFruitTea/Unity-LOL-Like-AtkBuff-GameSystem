using Core.Entity;
using Core.ECS;

namespace Gameplay.Combat.Targeting
{
    /// <summary>MVP：<see cref="CombatBoardLiteComponent.AttackTargetEntityId"/> ↔ 场景宿主。</summary>
    public static class CombatBoardTargetSync
    {
        public static bool TryGetPrimaryAttackTarget(EntityBase caster, out EntityBase target)
        {
            target = null;
            if (caster == null)
                return false;

            var ecs = caster.BoundEcsEntity;
            if (!ecs.IsValid() || !ecs.HasComponent<CombatBoardLiteComponent>())
                return false;

            long id = ecs.GetComponent<CombatBoardLiteComponent>().AttackTargetEntityId;
            if (id == 0)
                return false;

            return EntityEcsLinkRegistry.TryGetEntityBase(new EcsEntity(id), out target);
        }

        public static bool SetPrimaryAttackTarget(EntityBase caster, long targetEcsId)
        {
            if (caster == null)
                return false;

            var ecs = caster.BoundEcsEntity;
            if (!ecs.IsValid() || !ecs.HasComponent<CombatBoardLiteComponent>())
                return false;

            var board = ecs.GetComponent<CombatBoardLiteComponent>();
            board.AttackTargetEntityId = targetEcsId;
            ecs.SetComponent(board);
            return true;
        }

        /// <summary>主攻与仇恨同槽写入（塔/近战索敌对齐用）。</summary>
        public static bool SetAttackAndThreatSameTarget(EntityBase caster, long targetEcsId)
        {
            if (caster == null)
                return false;

            var ecs = caster.BoundEcsEntity;
            if (!ecs.IsValid() || !ecs.HasComponent<CombatBoardLiteComponent>())
                return false;

            var board = ecs.GetComponent<CombatBoardLiteComponent>();
            board.AttackTargetEntityId = targetEcsId;
            board.ThreatTargetEntityId = targetEcsId;
            ecs.SetComponent(board);
            return true;
        }
    }
}
