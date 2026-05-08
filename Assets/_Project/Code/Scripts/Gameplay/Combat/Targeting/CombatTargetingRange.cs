using Core.Entity;
using Core.ECS;
using UnityEngine;

namespace Gameplay.Combat.Targeting
{
    /// <summary>与选敌、普攻距离：<see cref="ResolveForCaster"/> 读 ECS/入参 <b>原始值</b>；世界距离换算见 <see cref="StoredAttackDistanceToWorldDistance"/>。</summary>
    public static class CombatTargetingRange
    {
        /// <summary>逻辑单位 ÷ 该值 ⇒ Unity 世界单位长度（常用 100cm/100 整数表值）。</summary>
        public const float DefaultAttackDistanceLogicUnitsPerWorldMeter = 100f;

        /// <summary>
        /// 存盘值 ≥ 本阈值时视为「表/数值距离」并做除法；小于则视为已是世界单位（如 1.5 近战）。
        /// </summary>
        public const float DefaultTreatStoredAttackDistanceAsLogicalWhenAtLeast = 100f;

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

        /// <summary>
        /// <paramref name="stored"/> 为 EntityData、塔 <c>AggroAcquireRange</c> 等存档数：<br/>
        /// · 若 <paramref name="stored"/> ≥ <paramref name="treatAsLogicWhenAtLeast"/> → 世界距离 = <paramref name="stored"/> / <paramref name="divisor"/><br/>
        /// · 否则 → 世界距离 = <paramref name="stored"/>（已是米等世界单位）。
        /// </summary>
        public static float StoredAttackDistanceToWorldDistance(
            float stored,
            float divisor = DefaultAttackDistanceLogicUnitsPerWorldMeter,
            float treatAsLogicWhenAtLeast = DefaultTreatStoredAttackDistanceAsLogicalWhenAtLeast)
        {
            if (stored < 1e-6f)
                return 0f;
            if (stored < treatAsLogicWhenAtLeast)
                return stored;
            if (divisor < 1e-6f)
                return Mathf.Max(0f, stored);

            return stored / divisor;
        }

        /// <summary><see cref="ResolveForCaster"/> 再换算为世界距离。</summary>
        public static float ResolveForCasterWorldDistance(
            EntityBase caster,
            float rangeOrRadius,
            float divisor = DefaultAttackDistanceLogicUnitsPerWorldMeter,
            float treatAsLogicWhenAtLeast = DefaultTreatStoredAttackDistanceAsLogicalWhenAtLeast)
        {
            float raw = ResolveForCaster(caster, rangeOrRadius);
            return StoredAttackDistanceToWorldDistance(raw, divisor, treatAsLogicWhenAtLeast);
        }

        /// <summary>等价于 <see cref="StoredAttackDistanceToWorldDistance"/>（地面圆环等与战斗共用同一换算）。</summary>
        public static float GameAttackDistanceToWorldDisplayRadius(
            float storedAttackDistanceLogical,
            float divisor = DefaultAttackDistanceLogicUnitsPerWorldMeter,
            float treatAsLogicWhenAtLeast = DefaultTreatStoredAttackDistanceAsLogicalWhenAtLeast)
        {
            return StoredAttackDistanceToWorldDistance(storedAttackDistanceLogical, divisor, treatAsLogicWhenAtLeast);
        }

        /// <summary><see cref="ResolveForCasterWorldDistance"/> 别名，供 UI/线框调用。</summary>
        public static float ResolveForCasterWorldDisplayRadius(
            EntityBase caster,
            float rangeOrRadius,
            float divisor = DefaultAttackDistanceLogicUnitsPerWorldMeter,
            float treatAsLogicWhenAtLeast = DefaultTreatStoredAttackDistanceAsLogicalWhenAtLeast)
        {
            return ResolveForCasterWorldDistance(caster, rangeOrRadius, divisor, treatAsLogicWhenAtLeast);
        }
    }
}
