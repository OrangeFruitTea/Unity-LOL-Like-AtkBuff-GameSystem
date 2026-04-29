using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gameplay.Equipment.Config
{
    public sealed class CraftMaterialEntryDto
    {
        [JsonProperty("itemConfigId")]
        public int ItemConfigId { get; set; }

        /// <summary> 消耗同名配置物品的件数（按堆叠/格计算）。 </summary>
        [JsonProperty("count")]
        public int Count { get; set; } = 1;
    }

    /// <summary>
    /// 静态合成配方（小件→大件）；与经济扣费拆分：<see cref="GoldCost"/> 为<strong>手续费</strong>，
    /// 成品仍走 <see cref="ItemConfigDefinition.BasePrice"/> 作为商店直购标价（文档 §6.8）。
    /// </summary>
    public sealed class CraftRecipeDefinitionDto
    {
        [JsonProperty("recipeId")]
        public int RecipeId { get; set; }

        [JsonProperty("resultItemConfigId")]
        public int ResultItemConfigId { get; set; }

        /// <summary> 手续费（局中金币）；不包含材料「原价」。 </summary>
        [JsonProperty("goldCost")]
        public int GoldCost { get; set; }

        [JsonProperty("materials")]
        public List<CraftMaterialEntryDto> Materials { get; set; } = new List<CraftMaterialEntryDto>();

        /// <summary> 可选禁用某条配方。 </summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;
    }
}
