using System.Collections.Generic;
using Core.Entity;

namespace Gameplay.Skill.Context
{
    /// <summary>
    /// 单次施法上下文：由战斗/输入层构造，交给技能执行与管线。
    /// </summary>
    public sealed class SkillCastContext
    {
        public SkillCastContext(EntityBase caster)
        {
            Caster = caster;
            SecondaryTargets = new List<EntityBase>();
        }

        public EntityBase Caster { get; }
        public EntityBase PrimaryTarget { get; set; }
        public List<EntityBase> SecondaryTargets { get; }

        public int SkillLevel { get; set; } = 1;
        public int RandomSeed { get; set; }
        public float CastStartedUnityTime { get; set; }

        public bool Validate(out string error)
        {
            error = null;
            if (Caster == null)
            {
                error = "Caster is null";
                return false;
            }

            if (!EntityEcsBridge.IsValidBuffTarget(Caster))
            {
                error = "Caster is not valid for skill/buff pipeline";
                return false;
            }

            return true;
        }
    }
}
