using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Equipment.Config;
using UnityEngine;

namespace Gameplay.Shop
{
    /// <summary>
    /// 合并 <see cref="ShopCatalogEntryDto"/> 与 <see cref="EquipmentCatalog"/> 的运行时条目。
    /// </summary>
    public sealed class ResolvedShopEntry
    {
        public int EntryId { get; set; }

        public string CategoryOrEmpty { get; set; }

        public int SortOrder { get; set; }

        public int ItemConfigId { get; set; }

        public ItemConfigDefinition ItemDefinition { get; set; }

        /// <summary> 展示售价：来自物品 <see cref="ItemConfigDefinition.BasePrice"/>。 </summary>
        public int DisplayPriceOrZero => ItemDefinition?.BasePrice ?? 0;

        /// <summary> 配置项存在且在目录中可被解析时为 true。 </summary>
        public bool IsResolvable => ItemDefinition != null;

        /// <summary>
        /// 表中 <c>resultItemConfigId == ItemConfigId</c> 且 <c>enabled</c> 的配方 id（供「商店内合成」入口与校验）。
        /// 空表示该商品无静态合成路径或仅能直购。
        /// </summary>
        public IReadOnlyList<int> CraftRecipeIds { get; set; } = Array.Empty<int>();
    }

    /// <summary> 商店目录查询（参见设计文档 §7 ShopCatalogService）。 </summary>
    public static class ShopCatalog
    {
        private static readonly List<ResolvedShopEntry> Entries = new List<ResolvedShopEntry>();

        public static IReadOnlyList<ResolvedShopEntry> All => Entries;

        public static void RebuildFrom(EquipmentDataFileDto dto)
        {
            Entries.Clear();
            var craftIndex = BuildCraftRecipeIndexByResultItem(dto);
            if (dto?.ShopEntries == null || dto.ShopEntries.Count == 0)
                return;

            var ordered = dto.ShopEntries.OrderBy(e => e.SortOrder).ThenBy(e => e.EntryId);
            foreach (var row in ordered)
            {
                var recipeIds = CraftIdsForItem(craftIndex, row.ItemConfigId);
                if (!EquipmentCatalog.TryGet(row.ItemConfigId, out var def))
                {
                    Debug.LogWarning($"[ShopCatalog] ShopEntries entry={row.EntryId} 引用缺失 item={row.ItemConfigId}");
                    Entries.Add(new ResolvedShopEntry
                    {
                        EntryId = row.EntryId,
                        CategoryOrEmpty = row.Category ?? "",
                        SortOrder = row.SortOrder,
                        ItemConfigId = row.ItemConfigId,
                        ItemDefinition = null,
                        CraftRecipeIds = recipeIds,
                    });
                    continue;
                }

                Entries.Add(new ResolvedShopEntry
                {
                    EntryId = row.EntryId,
                    CategoryOrEmpty = row.Category ?? "",
                    SortOrder = row.SortOrder,
                    ItemConfigId = row.ItemConfigId,
                    ItemDefinition = def,
                    CraftRecipeIds = recipeIds,
                });
            }
        }

        /// <summary> 解析 <paramref name="entryId"/>（<see cref="ShopCatalogEntryDto.EntryId"/>）。 </summary>
        public static bool TryGetEntry(int entryId, out ResolvedShopEntry entry)
        {
            for (var i = 0; i < Entries.Count; i++)
            {
                var e = Entries[i];
                if (e.EntryId == entryId)
                {
                    entry = e;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        private static Dictionary<int, List<int>> BuildCraftRecipeIndexByResultItem(EquipmentDataFileDto dto)
        {
            var map = new Dictionary<int, List<int>>();
            var list = dto?.CraftRecipes;
            if (list == null || list.Count == 0)
                return map;

            foreach (var r in list)
            {
                if (r == null || !r.Enabled)
                    continue;

                var resultId = r.ResultItemConfigId;
                if (!map.TryGetValue(resultId, out var ids))
                {
                    ids = new List<int>();
                    map[resultId] = ids;
                }

                ids.Add(r.RecipeId);
            }

            return map;
        }

        private static IReadOnlyList<int> CraftIdsForItem(Dictionary<int, List<int>> map, int itemConfigId)
        {
            if (!map.TryGetValue(itemConfigId, out var ids) || ids == null || ids.Count == 0)
                return Array.Empty<int>();

            var copy = new int[ids.Count];
            for (var i = 0; i < ids.Count; i++)
                copy[i] = ids[i];

            return copy;
        }

        /// <summary> category 为 null/空字符串时返回全部可解析条目。 </summary>
        public static IEnumerable<ResolvedShopEntry> GetFiltered(string categoryExactOrNullForAll)
        {
            IEnumerable<ResolvedShopEntry> seq = Entries.Where(e => e.IsResolvable);
            if (!string.IsNullOrEmpty(categoryExactOrNullForAll))
                seq = seq.Where(e => string.Equals(e.CategoryOrEmpty, categoryExactOrNullForAll, System.StringComparison.OrdinalIgnoreCase));

            return seq;
        }
    }
}
