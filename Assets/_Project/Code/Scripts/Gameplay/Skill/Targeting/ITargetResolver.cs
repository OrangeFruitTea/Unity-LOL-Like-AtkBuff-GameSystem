using System.Collections.Generic;
using Core.Entity;
using Gameplay.Skill.Config;
using Gameplay.Skill.Context;

namespace Gameplay.Skill.Targeting
{
    public interface ITargetResolver
    {
        IReadOnlyList<EntityBase> Resolve(SkillCastContext context, SkillTargetSelectorKind selector);
    }
}
