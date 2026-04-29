using System.Collections.Generic;
using System.Linq;
using Core.Entity;

namespace Gameplay.Skill.Buff
{
    /// <summary>
    /// 泛型 Buff 工厂，与 <see cref="BuffManager.AddBuff{T}"/> 及 <see cref="BuffBase.Init"/> 参数约定一致。
    /// </summary>
    public sealed class TypedBuffFactory<TBuff> : IBuffFactory where TBuff : BuffBase, new()
    {
        public void Apply(
            EntityBase target,
            EntityBase provider,
            uint level,
            float? durationOverride,
            IReadOnlyList<object> customArgsTail)
        {
            var args = new List<object> { provider, target, level };
            if (durationOverride.HasValue)
                args.Add(durationOverride.Value);
            if (customArgsTail != null && customArgsTail.Count > 0)
                args.AddRange(customArgsTail);

            BuffManager.Instance.AddBuff<TBuff>(target, provider, level, args.ToArray());
        }
    }
}
