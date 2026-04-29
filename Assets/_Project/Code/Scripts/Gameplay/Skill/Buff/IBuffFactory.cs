using System.Collections.Generic;
using Core.Entity;

namespace Gameplay.Skill.Buff
{
    /// <summary>
    /// 将「表驱动 buffId」映射为对 <see cref="BuffManager"/> 的一次施加。
    /// </summary>
    public interface IBuffFactory
    {
        void Apply(
            EntityBase target,
            EntityBase provider,
            uint level,
            float? durationOverride,
            IReadOnlyList<object> customArgsTail);
    }
}
