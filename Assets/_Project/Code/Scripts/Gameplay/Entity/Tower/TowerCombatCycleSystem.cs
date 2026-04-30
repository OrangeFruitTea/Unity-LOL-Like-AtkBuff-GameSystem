using Core.Combat;
using Core.Gameplay;
using Core.ECS;
using UnityEngine;

namespace Core.Entity
{
    /// <summary>
    /// 防御塔 **时间轴** → 索敌写 <see cref="CombatBoardLiteComponent"/> → Strike 接轨 <see cref="ImpactSystem"/>。<br/>
    /// 设计文档 §5.3、§10 顺序（本系统优先于 Impact）。
    /// </summary>
    public sealed class TowerCombatCycleSystem : IEcsSystem
    {
        private ImpactManager _impacts;

        public int UpdateOrder => 30;

        public void Initialize()
        {
            _impacts = new ImpactManager(EcsWorld.Instance);
        }

        public void Destroy()
        {
            _impacts = null;
        }

        public void Update()
        {
            if (_impacts == null)
                return;

            var world = EcsWorld.Instance;
            foreach (var ecs in world.GetEntitiesWithComponent<TowerCombatCycleComponent>())
            {
                if (!ecs.HasComponent<TowerModuleComponent>() ||
                    !ecs.HasComponent<CombatBoardLiteComponent>() ||
                    !ecs.HasComponent<FactionComponent>())
                    continue;

                var module = ecs.GetComponent<TowerModuleComponent>();
                var cycle = ecs.GetComponent<TowerCombatCycleComponent>();
                var board = ecs.GetComponent<CombatBoardLiteComponent>();
                var faction = ecs.GetComponent<FactionComponent>();
                float now = Time.time;

                if (!ecs.HasComponent<EntityDataComponent>())
                    continue;

                if (cycle.Phase != TowerCombatPhase.IdleScan &&
                    cycle.Phase != TowerCombatPhase.Interrupted)
                {
                    if (board.AttackTargetEntityId == 0 ||
                        !TowerTargetPolicy.IsValidAggroTarget(
                            ecs, new EcsEntity(board.AttackTargetEntityId), faction.TeamId, module))
                    {
                        Interrupt(ref cycle, ref board, ecs, now);
                    }
                }

                switch (cycle.Phase)
                {
                    case TowerCombatPhase.IdleScan:
                    case TowerCombatPhase.Interrupted:
                        if (CombatTargetAcquire.TryPickNearestHostileInRange(
                                ecs, faction.TeamId, module.AggroAcquireRange, out var picked))
                        {
                            board.AttackTargetEntityId = picked.Id;
                            board.ThreatTargetEntityId = picked.Id;
                            ecs.SetComponent(board);
                            cycle.Phase = TowerCombatPhase.Warning;
                            cycle.PhaseEndsAt = now + module.WarningDuration;
                            cycle.PendingImpactAt = -1f;
                        }
                        else if (cycle.Phase == TowerCombatPhase.Interrupted)
                        {
                            cycle.Phase = TowerCombatPhase.IdleScan;
                        }

                        ecs.SetComponent(cycle);
                        break;

                    case TowerCombatPhase.Warning:
                        if (now < cycle.PhaseEndsAt)
                            break;
                        cycle.Phase = TowerCombatPhase.LockPhase;
                        cycle.PhaseEndsAt = now + module.LockDuration;
                        ecs.SetComponent(cycle);
                        break;

                    case TowerCombatPhase.LockPhase:
                        if (now < cycle.PhaseEndsAt)
                            break;
                        cycle.Phase = TowerCombatPhase.StrikePending;
                        cycle.PendingImpactAt = now + module.StrikeHitDelay;
                        ecs.SetComponent(cycle);
                        break;

                    case TowerCombatPhase.StrikePending:
                        if (cycle.PendingImpactAt < 0f)
                            cycle.PendingImpactAt = now;
                        if (now < cycle.PendingImpactAt)
                            break;
                        if (board.AttackTargetEntityId != 0)
                        {
                            var tgt = new EcsEntity(board.AttackTargetEntityId);
                            if (TowerTargetPolicy.IsValidAggroTarget(ecs, tgt, faction.TeamId, module))
                            {
                                float raw = (float)ecs.GetComponent<EntityDataComponent>()
                                    .GetData(EntityBaseDataCore.AtkAD);
                                _impacts.CreateImpactEvent(
                                    ecs,
                                    tgt,
                                    TargetAttribute.Hp,
                                    raw,
                                    ImpactOperationType.Subtract,
                                    ImpactType.Physical,
                                    ImpactSourceType.NormalAtk);
                            }
                        }

                        cycle.Phase = TowerCombatPhase.Cooldown;
                        cycle.PhaseEndsAt = now + module.AttackCooldown;
                        cycle.PendingImpactAt = -1f;
                        ecs.SetComponent(cycle);
                        break;

                    case TowerCombatPhase.Cooldown:
                        if (now < cycle.PhaseEndsAt)
                            break;
                        board.AttackTargetEntityId = 0;
                        board.ThreatTargetEntityId = 0;
                        ecs.SetComponent(board);
                        cycle.Phase = TowerCombatPhase.IdleScan;
                        cycle.PhaseEndsAt = now;
                        ecs.SetComponent(cycle);
                        break;
                }
            }
        }

        private static void Interrupt(
            ref TowerCombatCycleComponent cycle,
            ref CombatBoardLiteComponent board,
            EcsEntity ecs,
            float now)
        {
            board.AttackTargetEntityId = 0;
            board.ThreatTargetEntityId = 0;
            ecs.SetComponent(board);
            cycle.Phase = TowerCombatPhase.Interrupted;
            cycle.PendingImpactAt = -1f;
            cycle.PhaseEndsAt = now;
            ecs.SetComponent(cycle);
        }
    }

    /// <summary> 目标是否仍在索敌球内且存活（毕设规则）。 </summary>
    internal static class TowerTargetPolicy
    {
        public static bool IsValidAggroTarget(
            EcsEntity tower,
            EcsEntity candidate,
            FactionTeamId towerFaction,
            TowerModuleComponent module)
        {
            if (!candidate.IsValid())
                return false;
            if (!candidate.HasComponent<EntityDataComponent>() || !candidate.HasComponent<FactionComponent>())
                return false;
            var data = candidate.GetComponent<EntityDataComponent>();
            if (data.GetData(EntityBaseDataCore.CrtHp) <= 1e-9)
                return false;
            if (!CombatHostility.AreHostile(towerFaction, candidate.GetComponent<FactionComponent>().TeamId))
                return false;

            if (!EntityEcsLinkRegistry.TryGetEntityBase(tower, out var ego) ||
                !EntityEcsLinkRegistry.TryGetEntityBase(candidate, out var other))
                return false;

            float r = module.AggroAcquireRange;
            return (ego.transform.position - other.transform.position).sqrMagnitude <= r * r;
        }
    }
}
