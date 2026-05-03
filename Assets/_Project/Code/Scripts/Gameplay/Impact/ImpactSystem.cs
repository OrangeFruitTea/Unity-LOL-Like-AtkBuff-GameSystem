using System;
using System.Collections.Generic;
using System.Linq;
using Core.ECS;
using Core.Entity;
using Gameplay.Presentation;
using UnityEngine;

namespace Core.Combat
{
    public class ImpactSystem : IEcsSystem
    {
        private HashSet<string> _processedEvents = new HashSet<string>();

        public int UpdateOrder => 31;

        public void Initialize()
        {
            _processedEvents.Clear();
        }

        public void Destroy()
        {
            _processedEvents.Clear();
        }

        public void Update()
        {
            Update(Time.deltaTime, EcsWorld.Instance);
        }

        private void Update(float deltaTime, EcsWorld world)
        {
            // 从ECS世界中获取所有具有ImpactEventComponent的实体
            var eventEntities = world.GetEntitiesWithComponent<ImpactEventComponent>();
            
            foreach (var eventEntity in eventEntities)
            {
                var impactEvent = eventEntity.GetComponent<ImpactEventComponent>();
                
                // 跳过已处理的事件
                if (impactEvent.IsProcessed || _processedEvents.Contains(impactEvent.EventId))
                {
                    continue;
                }
                
                // 确保来源和目标有效
                if (!impactEvent.Source.IsValid() || !impactEvent.Target.IsValid())
                {
                    // 标记为已处理并清理
                    impactEvent.IsProcessed = true;
                    eventEntity.SetComponent(impactEvent);
                    _processedEvents.Add(impactEvent.EventId);
                    continue;
                }
                
                // 检查目标是否有EntityDataComponent
                if (!impactEvent.Target.HasComponent<EntityDataComponent>())
                {
                    // 标记为已处理并清理
                    impactEvent.IsProcessed = true;
                    eventEntity.SetComponent(impactEvent);
                    _processedEvents.Add(impactEvent.EventId);
                    continue;
                }
                
                // 执行战斗判定流程
                var hitResult = PerformHitCheck(impactEvent, world);
                if (hitResult != HitResult.Hit)
                {
                    // 标记为已处理并清理
                    impactEvent.IsProcessed = true;
                    eventEntity.SetComponent(impactEvent);
                    _processedEvents.Add(impactEvent.EventId);
                    continue;
                }
                
                // 检查是否有ImpactValueComponent
                float baseValue = 0;
                if (eventEntity.HasComponent<ImpactValueComponent>())
                {
                    var valueComponent = eventEntity.GetComponent<ImpactValueComponent>();
                    baseValue = valueComponent.BaseValue;
                }
                
                // 执行暴击判断
                impactEvent.IsCritical = CheckCritical(impactEvent, world);
                if (impactEvent.IsCritical)
                {
                    baseValue *= GetCriticalMultiplier(impactEvent, world);
                }
                
                // 统一数值结算
                float finalValue = CalculateFinalValue(impactEvent, baseValue, world);
                
                // 将最终数值安全应用到角色身上
                ApplyImpactToTarget(impactEvent, finalValue, world);
                
                // 标记事件已处理
                impactEvent.IsProcessed = true;
                eventEntity.SetComponent(impactEvent);
                _processedEvents.Add(impactEvent.EventId);
                
                // 清理临时事件实体
                world.DestroyEntity(eventEntity);
            }
            
            // 清理过期的处理记录
            CleanupProcessedEvents();
        }
        
        private HitResult PerformHitCheck(ImpactEventComponent impactEvent, EcsWorld world)
        {
            // 简化的命中检查，实际游戏中可能需要更复杂的逻辑
            // 这里可以添加闪避、抵抗等判断
            return HitResult.Hit;
        }
        
        private bool CheckCritical(ImpactEventComponent impactEvent, EcsWorld world)
        {
            // 检查来源是否有暴击率属性
            if (impactEvent.Source.HasComponent<EntityDataComponent>())
            {
                var sourceData = impactEvent.Source.GetComponent<EntityDataComponent>();
                float criticalRate = (float)sourceData.GetData(EntityBaseDataCore.CriticalRate);
                
                // 简单的暴击判断
                return UnityEngine.Random.value <= criticalRate;
            }
            
            return false;
        }
        
        private float GetCriticalMultiplier(ImpactEventComponent impactEvent, EcsWorld world)
        {
            // 检查来源是否有暴击伤害属性
            if (impactEvent.Source.HasComponent<EntityDataComponent>())
            {
                var sourceData = impactEvent.Source.GetComponent<EntityDataComponent>();
                return (float)sourceData.GetData(EntityBaseData.CriticalDamage);
            }
            
            return 1.5f; // 默认暴击伤害150%
        }
        
        private float CalculateFinalValue(ImpactEventComponent impactEvent, float baseValue, EcsWorld world)
        {
            float finalValue = baseValue;
            
            // 应用所有修正器
            if (impactEvent.Target.HasComponent<ImpactModifierComponent>())
            {
                var modifierComponent = impactEvent.Target.GetComponent<ImpactModifierComponent>();
                
                // 按优先级排序修正器
                var sortedModifiers = modifierComponent.Modifiers
                    .OrderByDescending(m => m.Priority)
                    .ToList();
                
                foreach (var modifier in sortedModifiers)
                {
                    if (modifier.IsPercentage)
                    {
                        finalValue *= (1 + modifier.Value);
                    }
                    else
                    {
                        finalValue += modifier.Value;
                    }
                }
            }
            
            // 应用防御修正
            if (impactEvent.ImpactType == ImpactType.Physical)
            {
                if (impactEvent.Target.HasComponent<EntityDataComponent>())
                {
                    var targetData = impactEvent.Target.GetComponent<EntityDataComponent>();
                    float defence = (float)targetData.GetData(EntityBaseDataCore.DefenceAD);
                    // 简化的物理伤害计算
                    finalValue *= (100 / (100 + defence));
                }
            }
            else if (impactEvent.ImpactType == ImpactType.Magical)
            {
                if (impactEvent.Target.HasComponent<EntityDataComponent>())
                {
                    var targetData = impactEvent.Target.GetComponent<EntityDataComponent>();
                    float magicResist = (float)targetData.GetData(EntityBaseDataCore.DefenceAP);
                    // 简化的魔法伤害计算
                    finalValue *= (100 / (100 + magicResist));
                }
            }
            // 真实伤害不需要防御修正
            
            return finalValue;
        }
        
        private void ApplyImpactToTarget(ImpactEventComponent impactEvent, float finalValue, EcsWorld world)
        {
            var targetEntity = impactEvent.Target;
            var targetData = targetEntity.GetComponent<EntityDataComponent>();
            
            switch (impactEvent.TargetAttribute)
            {
                case TargetAttribute.Hp:
                {
                    var hpBefore = targetData.GetData(EntityBaseDataCore.CrtHp);
                    ApplyHpChange(targetData, finalValue, impactEvent.OperationType);
                    var hpAfter = targetData.GetData(EntityBaseDataCore.CrtHp);
                    if (impactEvent.OperationType == ImpactOperationType.Subtract && finalValue > 0f)
                    {
                        Debug.Log(
                            $"[ImpactSystem] HP 已扣 | targetEcs={targetEntity.Id} srcEcs={impactEvent.Source.Id} " +
                            $"finalDmg={finalValue:F2} crit={impactEvent.IsCritical} | hp {hpBefore:F1} → {hpAfter:F1}");
                    }

                    break;
                }
                case TargetAttribute.Mp:
                    ApplyMpChange(targetData, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.AtkAD:
                    ApplyCoreAttributeChange(targetData, EntityBaseDataCore.AtkAD, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.AtkAP:
                    ApplyCoreAttributeChange(targetData, EntityBaseDataCore.AtkAP, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.DefenceAD:
                    ApplyCoreAttributeChange(targetData, EntityBaseDataCore.DefenceAD, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.DefenceAP:
                    ApplyCoreAttributeChange(targetData, EntityBaseDataCore.DefenceAP, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.AtkSpeed:
                    ApplyCoreAttributeChange(targetData, EntityBaseDataCore.AtkSpeed, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.SkillCd:
                    ApplyCoreAttributeChange(targetData, EntityBaseDataCore.SkillCd, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.CriticalRate:
                    ApplyCoreAttributeChange(targetData, EntityBaseDataCore.CriticalRate, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.MoveSpeed:
                    ApplyCoreAttributeChange(targetData, EntityBaseDataCore.MoveSpeed, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.HpRecoverPerSecond:
                    ApplyBonusAttributeChange(targetData, EntityBaseData.HpRecoverPerSecond, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.MpRecoverPerSecond:
                    ApplyBonusAttributeChange(targetData, EntityBaseData.MpRecoverPerSecond, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.PenAD:
                    ApplyBonusAttributeChange(targetData, EntityBaseData.PenAD, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.PenAP:
                    ApplyBonusAttributeChange(targetData, EntityBaseData.PenAP, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.LifeSteal:
                    ApplyBonusAttributeChange(targetData, EntityBaseData.LifeSteal, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.OmniVamp:
                    ApplyBonusAttributeChange(targetData, EntityBaseData.OmniVamp, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.AtkDistance:
                    ApplyBonusAttributeChange(targetData, EntityBaseData.AtkDistance, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.CriticalDamage:
                    ApplyBonusAttributeChange(targetData, EntityBaseData.CriticalDamage, finalValue, impactEvent.OperationType);
                    break;
                case TargetAttribute.Resilience:
                    ApplyBonusAttributeChange(targetData, EntityBaseData.Resilience, finalValue, impactEvent.OperationType);
                    break;
                // 护盾和状态相关的处理可以在这里扩展
            }

            if (impactEvent.TargetAttribute == TargetAttribute.Hp &&
                impactEvent.OperationType == ImpactOperationType.Subtract &&
                finalValue > 0f &&
                impactEvent.Source.IsValid() &&
                targetEntity.HasComponent<CombatBoardLiteComponent>())
            {
                var board = targetEntity.GetComponent<CombatBoardLiteComponent>();
                board.LastDamageFromEntityId = impactEvent.Source.Id;
                targetEntity.SetComponent(board);
            }

            if (impactEvent.TargetAttribute == TargetAttribute.Hp &&
                impactEvent.OperationType == ImpactOperationType.Subtract &&
                finalValue > 0f)
                HitFxRelay.RaiseHpDamaged(impactEvent.Target, finalValue);
            
            // 更新目标实体的组件
            targetEntity.SetComponent(targetData);
        }
        
        private void ApplyHpChange(EntityDataComponent targetData, float value, ImpactOperationType operation)
        {
            double currentHp = targetData.GetData(EntityBaseDataCore.CrtHp);
            double maxHp = targetData.GetData(EntityBaseDataCore.HpLimit);
            double newValue = currentHp;
            
            switch (operation)
            {
                case ImpactOperationType.Add:
                    newValue = currentHp + value;
                    break;
                case ImpactOperationType.Subtract:
                    newValue = currentHp - value;
                    break;
                case ImpactOperationType.Override:
                    newValue = value;
                    break;
            }
            
            // 限制数值上下限
            newValue = Math.Max(0, Math.Min(newValue, maxHp));
            targetData.SetData(EntityBaseDataCore.CrtHp, newValue);
        }
        
        private void ApplyMpChange(EntityDataComponent targetData, float value, ImpactOperationType operation)
        {
            double currentMp = targetData.GetData(EntityBaseDataCore.CrtMp);
            double maxMp = targetData.GetData(EntityBaseDataCore.MpLimit);
            double newValue = currentMp;
            
            switch (operation)
            {
                case ImpactOperationType.Add:
                    newValue = currentMp + value;
                    break;
                case ImpactOperationType.Subtract:
                    newValue = currentMp - value;
                    break;
                case ImpactOperationType.Override:
                    newValue = value;
                    break;
            }
            
            // 限制数值上下限
            newValue = Math.Max(0, Math.Min(newValue, maxMp));
            targetData.SetData(EntityBaseDataCore.CrtMp, newValue);
        }
        
        private void ApplyCoreAttributeChange(EntityDataComponent targetData, EntityBaseDataCore attribute, float value, ImpactOperationType operation)
        {
            double currentValue = targetData.GetData(attribute);
            double newValue = currentValue;
            
            switch (operation)
            {
                case ImpactOperationType.Add:
                    newValue = currentValue + value;
                    break;
                case ImpactOperationType.Subtract:
                    newValue = currentValue - value;
                    break;
                case ImpactOperationType.Override:
                    newValue = value;
                    break;
            }
            
            // 限制数值下限
            newValue = Math.Max(0, newValue);
            targetData.SetData(attribute, newValue);
        }
        
        private void ApplyBonusAttributeChange(EntityDataComponent targetData, EntityBaseData attribute, float value, ImpactOperationType operation)
        {
            double currentValue = targetData.GetData(attribute);
            double newValue = currentValue;
            
            switch (operation)
            {
                case ImpactOperationType.Add:
                    newValue = currentValue + value;
                    break;
                case ImpactOperationType.Subtract:
                    newValue = currentValue - value;
                    break;
                case ImpactOperationType.Override:
                    newValue = value;
                    break;
            }
            
            // 限制数值下限
            newValue = Math.Max(0, newValue);
            targetData.SetData(attribute, newValue);
        }
        
        private void CleanupProcessedEvents()
        {
            // 清理处理记录，避免内存泄漏
            // 这里可以添加更复杂的清理逻辑，比如定期清理或达到一定数量时清理
            if (_processedEvents.Count > 1000)
            {
                _processedEvents.Clear();
            }
        }
    }
}
