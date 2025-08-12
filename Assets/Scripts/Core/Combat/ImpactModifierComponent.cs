using System.Collections.Generic;
using Core.ECS;

namespace Core.Combat
{
    public struct ImpactModifierComponent : IEcsComponent
    {
        public List<ImpactModifier> Modifiers;

        public void InitializeDefaults()
        {
            Modifiers = new List<ImpactModifier>();
        }
    }
}
