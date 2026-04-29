using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gameplay.Equipment.Config
{
    /// <summary>
    /// 装备/道具静态定义（表驱动，主效果通道为 <see cref="EquippedBuffs"/>）。
    /// </summary>
    public sealed class ItemConfigDefinition
    {
        [JsonProperty("itemConfigId")]
        public int ItemConfigId { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("slotType")]
        public string SlotType { get; set; }

        [JsonProperty("maxStack")]
        public int MaxStack { get; set; } = 1;

        [JsonProperty("uniqueItem")]
        public bool UniqueItem { get; set; }

        [JsonProperty("uniqueGroupId")]
        public string UniqueGroupId { get; set; }

        [JsonProperty("basePrice")]
        public int BasePrice { get; set; }

        [JsonProperty("purchasable")]
        public bool? Purchasable { get; set; }

        [JsonProperty("purchasePrerequisites")]
        public PurchasePrerequisitesDefinition PurchasePrerequisites { get; set; }

        [JsonProperty("equippedBuffs")]
        public List<EquipmentBuffBindingDefinition> EquippedBuffs { get; set; } = new List<EquipmentBuffBindingDefinition>();

        [JsonProperty("activeSkillId")]
        public int? ActiveSkillId { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>省略或 null：允许出售（按出售文档默认）。若为 false：不可向商店卖出。 </summary>
        [JsonProperty("sellable")]
        public bool? Sellable { get; set; }

        /// <summary>
        /// 出售时退还金币 = Floor(<see cref="BasePrice"/> * ratio)。省略或 ≤0：使用默认 0.5。
        /// </summary>
        [JsonProperty("sellRefundRatio")]
        public float? SellRefundRatio { get; set; }
    }
}
