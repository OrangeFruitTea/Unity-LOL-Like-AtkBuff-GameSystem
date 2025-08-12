using Core.ECS;

namespace Core.Combat
{
    public struct ImpactValueComponent : IEcsComponent
    {
        public float BaseValue;
        public float FinalValue;
        public ImpactType Type;
        public void InitializeDefaults()
        {
            BaseValue = 0;
            FinalValue = 0;
            Type = ImpactType.Physical;
        }
    }
}
