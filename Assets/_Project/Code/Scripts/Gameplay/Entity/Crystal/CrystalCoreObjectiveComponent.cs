using Core.ECS;

namespace Core.Entity
{
    /// <summary>
    /// 水晶（核心防御塔）裁定标记：持有者所属阵营在建筑被击倒时落败。<br/>
    /// 与一般塔共用节拍/伤害管线，仅在 Prefab 上额外挂 <see cref="CrystalObjectiveEcsAttachment"/> 注入本组件。<br/>
    /// 参见 <c>DesignDocuments/MOBA水晶与对局终局裁定设计文档.md</c> §4。
    /// </summary>
    public struct CrystalCoreObjectiveComponent : IEcsComponent
    {
        /// <summary>
        /// 该水晶所属的防守阵营；被摧毁则由 <see cref="CrystalMatchOutcomeBridge"/> 推导胜方。<br/>
        /// 对应 <see cref="FactionTeamId"/>（与 <see cref="FactionComponent"/> 一致时策划自检最简单）。
        /// </summary>
        public byte OwningTeamId;

        public void InitializeDefaults()
        {
            OwningTeamId = (byte)FactionTeamId.Blue;
        }
    }
}
