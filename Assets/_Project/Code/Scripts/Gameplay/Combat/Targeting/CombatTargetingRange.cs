using Core.Entity;
using Core.ECS;

namespace Gameplay.Combat.Targeting
{
    /// <summary>与选敌、普攻距离共用的射程解析（<paramref name="rangeOrRadius"/>≤0 用 <see cref="EntityBaseData.AtkDistance"/>）。</summary>
    public static class CombatTargetingRange
    {
        public static float ResolveForCaster(EntityBase caster, float rangeOrRadius)
        {
            if (rangeOrRadius > 1e-6f)
                return rangeOrRadius;

            if (caster == null)
                return 0f;

            var ecs = caster.BoundEcsEntity;
            if (!ecs.IsValid() || !ecs.HasComponent<EntityDataComponent>())
                return 0f;

            return (float)ecs.GetComponent<EntityDataComponent>().GetData(EntityBaseData.AtkDistance);
        }
    }
}
