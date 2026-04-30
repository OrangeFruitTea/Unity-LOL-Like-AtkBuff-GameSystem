using UnityEngine;

namespace Core.Entity
{
    /// <summary>
    /// 挂在 <see cref="EntityBase"/> Prefab 或实例上：<see cref="EntitySpawnSystem"/> 在 Create 后按其配置附加
    /// <see cref="FactionComponent"/>、<see cref="UnitArchetypeComponent"/>、可选 <see cref="CombatBoardLiteComponent"/>。
    /// 无此组件时保持仅 <see cref="EntityDataComponent"/>（与《MOBA局内单位模块ECS设计文档》§9 工厂推荐一致）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatEntitySpawnProfile : MonoBehaviour
    {
        [Tooltip("是否写入阵营（寻敌、Impact 敌对过滤等）")]
        public bool AddFaction = true;

        public FactionTeamId TeamId = FactionTeamId.Blue;

        [Tooltip("兵种原型 + 静态表主键")]
        public bool AddArchetype = true;

        public UnitArchetype Archetype = UnitArchetype.Hero;

        public int ConfigId;

        [Tooltip("战斗黑板：主攻/承伤/击杀等；接 Impact、UnitVitality 等")]
        public bool AddCombatBoardLite = true;
    }
}
