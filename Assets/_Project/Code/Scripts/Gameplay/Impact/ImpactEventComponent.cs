using Core.ECS;

namespace Core.Combat
{
    public struct ImpactEventComponent : IEcsComponent
    {
        public EcsEntity Source;
        public EcsEntity Target;
        public bool IsProcessed;
        public ImpactSourceType SourceType;
        public void InitializeDefaults()
        {
            Source = default;
            Target = default;
            IsProcessed = false;
            SourceType = ImpactSourceType.NormalAtk;
        }
    }
}
