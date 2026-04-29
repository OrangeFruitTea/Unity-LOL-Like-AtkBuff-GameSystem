using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gameplay.Equipment.Config
{
    /// <summary>
    /// StreamingAssets 装备表根对象（可与商店目录同文件或拆分）。
    /// </summary>
    public sealed class EquipmentDataFileDto
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [JsonProperty("items")]
        public List<ItemConfigDefinition> Items { get; set; } = new List<ItemConfigDefinition>();

        /// <summary> 可选：仅商店展示引用，运行时商店模块再消费。 </summary>
        [JsonProperty("shopEntries")]
        public List<ShopCatalogEntryDto> ShopEntries { get; set; }

        /// <summary>
        /// 可选：静态合成配方；见文档 §6.9、<see cref="CraftRecipeDefinitionDto"/>。
        /// </summary>
        [JsonProperty("craftRecipes")]
        public List<CraftRecipeDefinitionDto> CraftRecipes { get; set; }
    }

    public sealed class ShopCatalogEntryDto
    {
        [JsonProperty("entryId")]
        public int EntryId { get; set; }

        [JsonProperty("itemConfigId")]
        public int ItemConfigId { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("sortOrder")]
        public int SortOrder { get; set; }
    }
}
