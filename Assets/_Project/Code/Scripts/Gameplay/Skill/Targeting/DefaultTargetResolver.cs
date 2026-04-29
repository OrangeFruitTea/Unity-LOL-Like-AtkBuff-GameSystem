using System;
using System.Collections.Generic;
using Core.Entity;
using Gameplay.Skill.Config;
using Gameplay.Skill.Context;

namespace Gameplay.Skill.Targeting
{
    public sealed class DefaultTargetResolver : ITargetResolver
    {
        public IReadOnlyList<EntityBase> Resolve(SkillCastContext context, SkillTargetSelectorKind selector)
        {
            if (context == null)
                return Array.Empty<EntityBase>();

            switch (selector)
            {
                case SkillTargetSelectorKind.Caster:
                    return context.Caster != null ? new[] { context.Caster } : Array.Empty<EntityBase>();
                case SkillTargetSelectorKind.PrimaryTarget:
                    return context.PrimaryTarget != null ? new[] { context.PrimaryTarget } : Array.Empty<EntityBase>();
                case SkillTargetSelectorKind.SecondaryTargets:
                    return context.SecondaryTargets != null
                        ? context.SecondaryTargets
                        : Array.Empty<EntityBase>();
                default:
                    return Array.Empty<EntityBase>();
            }
        }
    }
}
