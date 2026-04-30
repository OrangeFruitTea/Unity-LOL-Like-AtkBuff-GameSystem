using System.Collections.Generic;
using Core.Entity;

namespace Gameplay.Skill.Buff
{
    /// <summary>
    /// 将表驱动 <c>buffId</c> 与 <see cref="MetaBuff"/> 连接；Init 参数顺序：<c>[provider, owner, level, duration, buffConfigId, …custom]</c>。
    /// </summary>
    public sealed class MetaBuffApplyFactory : IBuffFactory
    {
        private readonly int _buffConfigId;

        public MetaBuffApplyFactory(int buffConfigId)
        {
            _buffConfigId = buffConfigId;
        }

        public void Apply(
            EntityBase target,
            EntityBase provider,
            uint level,
            float? durationOverride,
            IReadOnlyList<object> customArgsTail)
        {
            var args = new List<object> { provider, target, level };
            float duration = durationOverride ?? 8f;
            args.Add(duration);
            args.Add(_buffConfigId);
            if (customArgsTail != null && customArgsTail.Count > 0)
                args.AddRange(customArgsTail);

            BuffManager.Instance.AddBuff<MetaBuff>(target, provider, level, args.ToArray());
        }
    }
}
