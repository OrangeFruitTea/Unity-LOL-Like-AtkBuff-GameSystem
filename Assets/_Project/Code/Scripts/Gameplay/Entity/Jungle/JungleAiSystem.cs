using System.Collections.Generic;
using Core.Combat;
using Core.Entity;
using Core.Gameplay;
using Core.ECS;
using UnityEngine;

namespace Core.Entity.Jungle
{
    /// <summary> 野怪：租赁圈内索敌 → 追击 → 超距回巢；近战用 <see cref="ImpactManager"/> 发普攻事件。 </summary>
    public sealed class JungleAiSystem : IEcsSystem
    {
        public int UpdateOrder => 38;

        private ImpactManager _impacts;
        private readonly Dictionary<long, float> _nextMeleeAt = new Dictionary<long, float>();

        public void Initialize()
        {
            _impacts = EcsWorld.Instance.CombatImpacts;
        }

        public void Destroy()
        {
            _impacts = null;
            _nextMeleeAt.Clear();
        }

        public void Update()
        {
            if (_impacts == null)
                return;

            float now = Time.time;

            foreach (var ecs in EcsWorld.Instance.GetEntitiesWithComponent<JungleCreepModuleComponent>())
            {
                if (!ecs.HasComponent<FactionComponent>() ||
                    !ecs.HasComponent<CombatBoardLiteComponent>() ||
                    !ecs.HasComponent<EntityDataComponent>())
                    continue;

                var data = ecs.GetComponent<EntityDataComponent>();
                if (data.GetData(EntityBaseDataCore.CrtHp) <= 1e-9)
                    continue;

                if (!EntityEcsLinkRegistry.TryGetEntityBase(ecs, out var host))
                    continue;

                var module = ecs.GetComponent<JungleCreepModuleComponent>();
                var board = ecs.GetComponent<CombatBoardLiteComponent>();
                var faction = ecs.GetComponent<FactionComponent>();
                Vector3 pos = host.transform.position;
                float leashR = module.LeashRadius;
                float distLeashSqr = (pos - module.LeashCenter).sqrMagnitude;
                float speed = (float)data.GetData(EntityBaseDataCore.MoveSpeed);

                switch (module.CurrentState)
                {
                    case JungleCreepState.Idle:
                        board.AttackTargetEntityId = 0;
                        if (CombatTargetAcquire.TryPickNearestHostileInRangeFromWorldPoint(
                                module.LeashCenter,
                                ecs,
                                faction.TeamId,
                                leashR,
                                out var picked))
                        {
                            module.CurrentState = JungleCreepState.Pursue;
                            board.AttackTargetEntityId = picked.Id;
                        }

                        ecs.SetComponent(module);
                        ecs.SetComponent(board);
                        break;

                    case JungleCreepState.Pursue:
                        if (distLeashSqr > leashR * leashR)
                        {
                            module.CurrentState = JungleCreepState.Returning;
                            board.AttackTargetEntityId = 0;
                            ecs.SetComponent(module);
                            ecs.SetComponent(board);
                            break;
                        }

                        var tgtId = board.AttackTargetEntityId;
                        var targetEcs = new EcsEntity(tgtId);
                        if (tgtId == 0 ||
                            !IsJungleTargetStillValid(targetEcs, module, faction.TeamId))
                        {
                            module.CurrentState = JungleCreepState.Idle;
                            board.AttackTargetEntityId = 0;
                            ecs.SetComponent(module);
                            ecs.SetComponent(board);
                            break;
                        }

                        if (EntityEcsLinkRegistry.TryGetEntityBase(targetEcs, out var tHost))
                        {
                            TryMeleeAttack(ecs, data, now, targetEcs);
                            host.transform.position = Vector3.MoveTowards(
                                pos,
                                tHost.transform.position,
                                speed * Time.deltaTime);
                        }

                        break;

                    case JungleCreepState.Returning:
                        board.AttackTargetEntityId = 0;
                        host.transform.position = Vector3.MoveTowards(
                            pos,
                            module.LeashCenter,
                            speed * Time.deltaTime);

                        if ((host.transform.position - module.LeashCenter).sqrMagnitude < 0.2f * 0.2f)
                            module.CurrentState = JungleCreepState.Idle;

                        ecs.SetComponent(module);
                        ecs.SetComponent(board);
                        break;
                }
            }
        }

        private void TryMeleeAttack(EcsEntity creep, EntityDataComponent data, float now, EcsEntity target)
        {
            if (!_nextMeleeAt.TryGetValue(creep.Id, out float readyAt))
                readyAt = 0f;
            if (now < readyAt)
                return;

            if (!EntityEcsLinkRegistry.TryGetEntityBase(creep, out var ego) ||
                !EntityEcsLinkRegistry.TryGetEntityBase(target, out var other))
                return;

            float atkRange = (float)data.GetData(EntityBaseData.AtkDistance);
            if ((ego.transform.position - other.transform.position).sqrMagnitude > atkRange * atkRange)
                return;

            float raw = (float)data.GetData(EntityBaseDataCore.AtkAD);
            _impacts.CreateImpactEvent(
                creep,
                target,
                TargetAttribute.Hp,
                raw,
                ImpactOperationType.Subtract,
                ImpactType.Physical,
                ImpactSourceType.NormalAtk);

            _nextMeleeAt[creep.Id] = now + 1.1f;
        }

        private static bool IsJungleTargetStillValid(
            EcsEntity target,
            JungleCreepModuleComponent module,
            FactionTeamId myFaction)
        {
            if (!target.IsValid())
                return false;
            if (!target.HasComponent<EntityDataComponent>() || !target.HasComponent<FactionComponent>())
                return false;

            var tData = target.GetComponent<EntityDataComponent>();
            if (tData.GetData(EntityBaseDataCore.CrtHp) <= 1e-9)
                return false;
            if (!CombatHostility.AreHostile(myFaction, target.GetComponent<FactionComponent>().TeamId))
                return false;

            if (!EntityEcsLinkRegistry.TryGetEntityBase(target, out var tHost))
                return false;

            float r = module.LeashRadius;
            return (tHost.transform.position - module.LeashCenter).sqrMagnitude <= r * r + 0.25f;
        }
    }
}
