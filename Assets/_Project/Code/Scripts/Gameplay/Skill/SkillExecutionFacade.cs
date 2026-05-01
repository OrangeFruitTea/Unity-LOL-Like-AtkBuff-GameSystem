using Core.ECS;
using Core.Entity;
using Gameplay.Presentation;
using Gameplay.Skill.Config;
using Gameplay.Skill.Context;
using Gameplay.Skill.Ecs;
using Gameplay.Skill.Runtime;
using UnityEngine;

namespace Gameplay.Skill
{
    /// <summary>
    /// 技能执行门面：校验目录与冷却，启动 <see cref="SkillCastPipelineSystem"/>。
    /// </summary>
    public static class SkillExecutionFacade
    {
        public static bool TryBeginCast(int skillId, SkillCastContext context, out string error) =>
            TryBeginCast(skillId, context, true, out error);

        public static bool TryBeginCast(int skillId, SkillCastContext context, bool respectCooldown, out string error)
        {
            error = null;
            if (!SkillCatalog.TryGet(skillId, out var def))
            {
                error = $"skill id {skillId} not found in SkillCatalog";
                return false;
            }

            if (!context.Validate(out error))
                return false;

            if (def.RequiresTarget && context.PrimaryTarget == null)
            {
                error = "skill requires PrimaryTarget";
                return false;
            }

            long ecsId = context.Caster.BoundEcsEntity.Id;
            if (ecsId == 0)
            {
                error = "caster BoundEcsEntity is invalid";
                return false;
            }

            if (respectCooldown && !SkillCooldownTracker.IsReady(ecsId, skillId))
            {
                error = "skill is on cooldown";
                return false;
            }

            context.CastStartedUnityTime = Time.time;
            var system = EcsWorld.Instance != null ? EcsWorld.Instance.GetEcsSystem<SkillCastPipelineSystem>() : null;
            if (system == null)
            {
                error = "SkillCastPipelineSystem is not registered on EcsWorld";
                return false;
            }

            if (!system.TryBegin(def, context, out error))
                return false;

            if (respectCooldown)
                SkillCooldownTracker.NotifyCast(ecsId, skillId, def.CooldownSeconds);

            context.Caster.GetComponent<UnitAnimDrv>()?.NotifySkillCastStarted();
            return true;
        }

        public static void CancelActivePipeline(EntityBase caster)
        {
            EcsWorld.Instance?.GetEcsSystem<SkillCastPipelineSystem>()?.CancelForCaster(caster);
        }
    }
}
