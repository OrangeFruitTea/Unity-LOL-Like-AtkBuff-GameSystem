using System;
using System.Collections.Generic;
using Core.ECS;
using Core.Entity;
using Gameplay.Skill.Buff;
using Gameplay.Skill.Conditions;
using Gameplay.Skill.Config;
using Gameplay.Skill.Context;
using Gameplay.Skill.Runtime;
using Gameplay.Skill.Targeting;
using UnityEngine;

namespace Gameplay.Skill.Ecs
{
    /// <summary>
    /// 驱动 Buff 施加管线：定时批次、条件步、取消。
    /// </summary>
    public sealed class SkillCastPipelineSystem : IEcsSystem
    {
        private sealed class SkillCastSession
        {
            public string CastInstanceId;
            public SkillDefinition Definition;
            public SkillCastContext Context;
            public List<ExecutionBatch> TimedBatches;
            public int NextBatchIndex;
            public HashSet<int> PendingConditionSteps;
            public HashSet<int> PendingEventSteps;
            public bool Cancelled;
            public bool LoggedEventStub;
        }

        public int UpdateOrder => 50;

        private readonly Dictionary<long, SkillCastSession> _sessions = new Dictionary<long, SkillCastSession>();
        private readonly ITargetResolver _targetResolver = new DefaultTargetResolver();

        public void Initialize()
        {
        }

        public void Destroy()
        {
            _sessions.Clear();
        }

        public void Update()
        {
            float now = Time.time;
            var done = new List<long>();
            foreach (var kv in _sessions)
            {
                var session = kv.Value;
                if (session.Cancelled)
                {
                    done.Add(kv.Key);
                    continue;
                }

                if (!IsContextAlive(session.Context))
                {
                    done.Add(kv.Key);
                    continue;
                }

                ProcessConditionSteps(session, now);
                ProcessTimedBatches(session, now);
                ProcessEventStub(session);

                if (IsSessionComplete(session))
                    done.Add(kv.Key);
            }

            foreach (var id in done)
                EndSession(id);
        }

        public bool TryBegin(SkillDefinition definition, SkillCastContext context, out string error)
        {
            error = null;
            if (definition == null)
            {
                error = "definition is null";
                return false;
            }

            if (context == null || context.Caster == null)
            {
                error = "context invalid";
                return false;
            }

            long id = context.Caster.BoundEcsEntity.Id;
            if (id == 0)
            {
                error = "caster has no EcsEntity";
                return false;
            }

            if (_sessions.ContainsKey(id))
            {
                error = "caster already has an active skill pipeline";
                return false;
            }

            var session = new SkillCastSession
            {
                CastInstanceId = Guid.NewGuid().ToString("N"),
                Definition = definition,
                Context = context,
                TimedBatches = PipelineScheduleBuilder.BuildTimedBatches(definition),
                NextBatchIndex = 0,
                PendingConditionSteps = new HashSet<int>(PipelineScheduleBuilder.CollectConditionStepIndices(definition)),
                PendingEventSteps = new HashSet<int>(PipelineScheduleBuilder.CollectEventStepIndices(definition))
            };

            _sessions[id] = session;
            SetPipelineComponentFlag(context.Caster, true);
            Debug.Log(
                $"[SkillCastPipeline] begin skill={definition.SkillId} cast={session.CastInstanceId} casterEcs={id}");
            return true;
        }

        public void CancelForCaster(EntityBase caster)
        {
            if (caster == null)
                return;
            long id = caster.BoundEcsEntity.Id;
            if (_sessions.TryGetValue(id, out var s))
                s.Cancelled = true;
        }

        private void EndSession(long casterEcsId)
        {
            if (!_sessions.TryGetValue(casterEcsId, out var session))
                return;
            _sessions.Remove(casterEcsId);
            if (session.Context?.Caster != null)
                SetPipelineComponentFlag(session.Context.Caster, false);
            Debug.Log($"[SkillCastPipeline] end cast={session.CastInstanceId} skill={session.Definition.SkillId}");
        }

        private static void SetPipelineComponentFlag(EntityBase caster, bool active)
        {
            var ecs = caster.BoundEcsEntity;
            if (!EcsWorld.Exists(ecs))
                return;

            SkillPipelineRuntimeComponent comp;
            if (EcsWorld.HasComponent<SkillPipelineRuntimeComponent>(ecs))
                comp = EcsWorld.GetComponent<SkillPipelineRuntimeComponent>(ecs);
            else
            {
                comp = new SkillPipelineRuntimeComponent();
                comp.InitializeDefaults();
            }

            comp.HasActiveCast = active;
            if (EcsWorld.HasComponent<SkillPipelineRuntimeComponent>(ecs))
                EcsWorld.SetComponent(ecs, comp);
            else
                EcsWorld.AddComponent(ecs, comp);
        }

        private static bool IsContextAlive(SkillCastContext ctx)
        {
            return ctx?.Caster != null && EntityEcsBridge.IsValidBuffTarget(ctx.Caster);
        }

        private void ProcessTimedBatches(SkillCastSession session, float now)
        {
            float t0 = session.Context.CastStartedUnityTime;
            while (session.NextBatchIndex < session.TimedBatches.Count)
            {
                var batch = session.TimedBatches[session.NextBatchIndex];
                if (now < t0 + batch.FireTimeFromCastStart)
                    break;

                foreach (int stepIndex in batch.StepIndices)
                    ExecuteStep(session, stepIndex);

                session.NextBatchIndex++;
            }
        }

        private void ProcessConditionSteps(SkillCastSession session, float _)
        {
            if (session.PendingConditionSteps.Count == 0)
                return;

            var fired = new List<int>();
            foreach (int idx in session.PendingConditionSteps)
            {
                var step = session.Definition.Steps[idx];
                if (!SkillConditionRegistry.TryEvaluate(step.ConditionId, session.Context, step))
                    continue;

                ExecuteStep(session, idx);
                fired.Add(idx);
            }

            foreach (int idx in fired)
                session.PendingConditionSteps.Remove(idx);
        }

        private void ProcessEventStub(SkillCastSession session)
        {
            if (session.PendingEventSteps.Count == 0 || session.LoggedEventStub)
                return;
            session.LoggedEventStub = true;
            Debug.LogWarning(
                $"[SkillCastPipeline] cast={session.CastInstanceId} 含有 OnEvent 步骤，当前版本未实现事件订阅，已忽略 {session.PendingEventSteps.Count} 步。");
            session.PendingEventSteps.Clear();
        }

        private bool IsSessionComplete(SkillCastSession session)
        {
            bool timedDone = session.NextBatchIndex >= session.TimedBatches.Count;
            bool condDone = session.PendingConditionSteps.Count == 0;
            bool eventDone = session.PendingEventSteps.Count == 0;
            return timedDone && condDone && eventDone;
        }

        private void ExecuteStep(SkillCastSession session, int stepIndex)
        {
            var def = session.Definition;
            var step = def.Steps[stepIndex];
            var ctx = session.Context;
            var targets = _targetResolver.Resolve(ctx, step.TargetSelector);
            uint level = SkillParameterResolver.ResolveBuffLevel(step, ctx.SkillLevel);

            foreach (var target in targets)
            {
                if (target == null || !EntityEcsBridge.IsValidBuffTarget(target))
                    continue;

                if (!BuffApplyService.TryApplyStep(step, target, ctx.Caster, level, null, out var err))
                    Debug.LogWarning(
                        $"[SkillCastPipeline] step={step.StepId} idx={stepIndex} buff={step.BuffId} err={err}");
            }
        }
    }
}
