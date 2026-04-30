using Core.ECS;

namespace Core.Entity
{
    /// <summary> 单位原型 + 静态表主键；与 Prefab 解耦。设计文档 §4.2。 </summary>
    public struct UnitArchetypeComponent : IEcsComponent
    {
        public UnitArchetype Archetype;

        public int ConfigId;

        public void InitializeDefaults()
        {
            Archetype = UnitArchetype.Hero;
            ConfigId = 0;
        }
    }
}
