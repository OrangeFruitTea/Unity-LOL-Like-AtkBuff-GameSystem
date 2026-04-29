using Gameplay.Skill.Config;
using UnityEngine;

namespace Gameplay.Skill
{
    /// <summary>
    /// 技能等级 → Buff 施加等级等解析。
    /// </summary>
    public static class SkillParameterResolver
    {
        public static uint ResolveBuffLevel(BuffApplicationStepDefinition step, int skillLevel)
        {
            if (step == null)
                return 1;
            int lv = Mathf.Max(1, skillLevel);
            return step.BuffLevel + (uint)(Mathf.Max(0, lv - 1) * step.LevelScalingPerSkillLevel);
        }
    }
}
