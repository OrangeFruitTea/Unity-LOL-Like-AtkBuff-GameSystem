using Core.ECS;
using Core.Combat;

namespace Core.Gameplay
{
    public class ImpactManager
    {
        private EcsWorld _world;

        /// <summary>与 <see cref="EcsWorld.CombatImpacts"/> 同一实例，全工程唯一入口。</summary>
        public static ImpactManager Shared => EcsWorld.Instance.CombatImpacts;
        
        public ImpactManager(EcsWorld world)
        {
            _world = world;
        }
        
        /// <summary>
        /// 创建一个新的impact事件
        /// </summary>
        public EcsEntity CreateImpactEvent(
            EcsEntity source,
            EcsEntity target,
            TargetAttribute targetAttribute,
            float value,
            ImpactOperationType operationType = ImpactOperationType.Add,
            ImpactType impactType = ImpactType.Physical,
            ImpactSourceType sourceType = ImpactSourceType.NormalAtk)
        {
            // 创建事件实体
            var eventEntity = _world.CreateEntity();
            
            // 添加ImpactEventComponent
            var eventComponent = new ImpactEventComponent();
            eventComponent.InitializeDefaults();
            eventComponent.Source = source;
            eventComponent.Target = target;
            eventComponent.TargetAttribute = targetAttribute;
            eventComponent.OperationType = operationType;
            eventComponent.ImpactType = impactType;
            eventComponent.SourceType = sourceType;
            eventEntity.AddComponent(eventComponent);
            
            // 添加ImpactValueComponent
            var valueComponent = new ImpactValueComponent();
            valueComponent.InitializeDefaults();
            valueComponent.BaseValue = value;
            valueComponent.Type = impactType;
            eventEntity.AddComponent(valueComponent);
            
            return eventEntity;
        }
        
        /// <summary>
        /// 为目标实体添加修饰器
        /// </summary>
        public void AddModifier(EcsEntity target, ImpactModifier modifier)
        {
            if (!target.IsValid())
                return;
            
            // 确保目标实体有ImpactModifierComponent
            if (!target.HasComponent<ImpactModifierComponent>())
            {
                var modifierComponent = new ImpactModifierComponent();
                modifierComponent.InitializeDefaults();
                target.AddComponent(modifierComponent);
            }
            
            // 添加修饰器
            var modifierComponent = target.GetComponent<ImpactModifierComponent>();
            modifierComponent.Modifiers.Add(modifier);
            target.SetComponent(modifierComponent);
        }
        
        /// <summary>
        /// 从目标实体移除修饰器
        /// </summary>
        public void RemoveModifier(EcsEntity target, string source)
        {
            if (!target.IsValid() || !target.HasComponent<ImpactModifierComponent>())
                return;
            
            var modifierComponent = target.GetComponent<ImpactModifierComponent>();
            modifierComponent.Modifiers.RemoveAll(m => m.Source == source);
            target.SetComponent(modifierComponent);
        }
        
        /// <summary>
        /// 清除目标实体的所有修饰器
        /// </summary>
        public void ClearModifiers(EcsEntity target)
        {
            if (!target.IsValid() || !target.HasComponent<ImpactModifierComponent>())
                return;
            
            var modifierComponent = target.GetComponent<ImpactModifierComponent>();
            modifierComponent.Modifiers.Clear();
            target.SetComponent(modifierComponent);
        }
    }
}
