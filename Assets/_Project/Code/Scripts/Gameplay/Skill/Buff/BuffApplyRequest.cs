using System.Collections.Generic;
using Core.Entity;
using Gameplay.Skill.Config;

namespace Gameplay.Skill.Buff
{
    /// <summary>
    /// 一次 Buff 施加请求（修饰器链输出仍为该结构）。
    /// </summary>
    public sealed class BuffApplyRequest
    {
        public int BuffId { get; set; }
        public EntityBase Target { get; set; }
        public EntityBase Provider { get; set; }
        public uint Level { get; set; } = 1;
        public float? DurationOverride { get; set; }
        public List<object> CustomArgsTail { get; set; }

        public static BuffApplyRequest FromStep(BuffApplicationStepDefinition step, EntityBase target, EntityBase provider, uint resolvedLevel)
        {
            return new BuffApplyRequest
            {
                BuffId = step.BuffId,
                Target = target,
                Provider = provider,
                Level = resolvedLevel,
                DurationOverride = step.DurationOverride,
                CustomArgsTail = step.CustomArgs != null ? new List<object>(step.CustomArgs) : new List<object>()
            };
        }
    }
}
