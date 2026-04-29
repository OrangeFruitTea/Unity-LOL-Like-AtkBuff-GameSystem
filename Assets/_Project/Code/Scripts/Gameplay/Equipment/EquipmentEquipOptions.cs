namespace Gameplay.Equipment
{
    /// <summary>
    /// 穿戴施加 Buff 时的可选策略（对齐设计文档 §6.2.1 严格模式与校验）。
    /// </summary>
    public struct EquipmentEquipOptions
    {
        /// <summary> 任一条 <see cref="Gameplay.Skill.Buff.BuffApplyService.TryApply"/> 失败时是否回滚本件已施加的 Buff 并返回失败。 </summary>
        public bool StrictEquipmentBuffApply;

        public bool ValidateAgainstBuffData;
        public bool ValidateRegistry;

        /// <summary> 用于解析 <c>buffLevel + levelScalingPerItemTier * tier</c>。 </summary>
        public uint ItemTier;

        public static EquipmentEquipOptions Default => new EquipmentEquipOptions
        {
            StrictEquipmentBuffApply = false,
            ValidateAgainstBuffData = true,
            ValidateRegistry = true,
            ItemTier = 0
        };
    }
}
