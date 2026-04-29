using System;
using System.Collections.Generic;
using Core.ECS;
using Core.Entity;
using Gameplay.Skill.Config;
using Gameplay.Skill.Context;

namespace Gameplay.Skill.Conditions
{
    public delegate bool SkillConditionFn(SkillCastContext context, BuffApplicationStepDefinition step);

    /// <summary>
    /// 条件 id → 谓词（<see cref="BuffApplicationTriggerKind.OnCondition"/>）。
    /// </summary>
    public static class SkillConditionRegistry
    {
        private static readonly Dictionary<string, SkillConditionFn> Conditions =
            new Dictionary<string, SkillConditionFn>(StringComparer.Ordinal);

        public static void Clear()
        {
            Conditions.Clear();
        }

        public static void Register(string conditionId, SkillConditionFn fn)
        {
            if (string.IsNullOrEmpty(conditionId) || fn == null)
                return;
            Conditions[conditionId] = fn;
        }

        public static bool TryEvaluate(string conditionId, SkillCastContext context, BuffApplicationStepDefinition step)
        {
            if (string.IsNullOrEmpty(conditionId))
                return true;
            if (!Conditions.TryGetValue(conditionId, out var fn))
                return false;
            try
            {
                return fn(context, step);
            }
            catch
            {
                return false;
            }
        }

        /// <summary> 内建：主目标当前生命比例 ≤ <see cref="BuffApplicationStepDefinition.ConditionParam"/>。 </summary>
        public static void RegisterBuiltInDefaults()
        {
            Register("entity_hp_below_ratio", (ctx, step) =>
            {
                if (ctx?.PrimaryTarget == null)
                    return false;
                var ecs = ctx.PrimaryTarget.BoundEcsEntity;
                if (!EcsWorld.Exists(ecs))
                    return false;
                var data = EcsWorld.GetComponent<EntityDataComponent>(ecs);
                double hp = data.GetData(EntityBaseDataCore.CrtHp);
                double max = data.GetData(EntityBaseDataCore.HpLimit);
                if (max <= 1e-6)
                    return false;
                return (hp / max) <= step.ConditionParam + 1e-5;
            });
        }
    }
}
