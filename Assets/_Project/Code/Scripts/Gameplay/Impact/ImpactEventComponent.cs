using Core.ECS;

namespace Core.Combat
{
    public struct ImpactEventComponent : IEcsComponent
    {
        public EcsEntity Source;
        public EcsEntity Target;
        public bool IsProcessed;
        public ImpactSourceType SourceType;
        public TargetAttribute TargetAttribute;
        public ImpactOperationType OperationType;
        public ImpactType ImpactType;
        public bool IsCritical;
        public string EventId;
        
        public void InitializeDefaults()
        {
            Source = default;
            Target = default;
            IsProcessed = false;
            SourceType = ImpactSourceType.NormalAtk;
            TargetAttribute = TargetAttribute.Hp;
            OperationType = ImpactOperationType.Add;
            ImpactType = ImpactType.Physical;
            IsCritical = false;
            EventId = System.Guid.NewGuid().ToString();
        }
    }
}

