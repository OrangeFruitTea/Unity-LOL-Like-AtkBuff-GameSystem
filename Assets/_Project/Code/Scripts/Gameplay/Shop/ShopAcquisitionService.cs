using System;
using Core.Entity;
using Gameplay.Equipment;

namespace Gameplay.Shop
{
    /// <summary>
    /// 商店条目的统一收口：金币直购 <see cref="PurchaseService.TryPurchase"/> 或与目录绑定的配方合成 <see cref="CraftingService.TryCraft"/>。
    /// </summary>
    public static class ShopAcquisitionService
    {
        /// <summary>
        /// <paramref name="mode"/>：<see cref="ShopAcquireMode.DirectGoldPurchase"/> 按 <see cref="ResolvedShopEntry.ItemConfigId"/> 直购；
        /// <see cref="ShopAcquireMode.CraftRecipe"/> 时 <paramref name="recipeId"/> 须属于该条目的 <see cref="ResolvedShopEntry.CraftRecipeIds"/> 且与生成的成品 id 一致。
        /// </summary>
        public static PurchaseResult TryAcquireFromShopEntry(
            EntityBase hero,
            int heroLevel,
            int shopEntryId,
            ShopAcquireMode mode,
            int recipeId,
            in EquipmentEquipOptions equipOptions)
        {
            if (hero == null)
                return PurchaseResult.Fail(ShopErrorCode.ItemNotFound, "hero is null");

            if (!ShopCatalog.TryGetEntry(shopEntryId, out var entry) || entry == null)
                return PurchaseResult.Fail(ShopErrorCode.ItemNotFound, $"entry {shopEntryId}");

            if (!entry.IsResolvable || entry.ItemDefinition == null)
                return PurchaseResult.Fail(ShopErrorCode.ItemNotFound, "entry unresolved");

            if (mode == ShopAcquireMode.DirectGoldPurchase)
            {
                var req = new PurchaseRequest
                {
                    Hero = hero,
                    HeroLevel = heroLevel,
                    ItemConfigId = entry.ItemConfigId,
                    PreferSlotIndex = 0,
                };
                return PurchaseService.TryPurchase(in req, in equipOptions);
            }

            if (mode != ShopAcquireMode.CraftRecipe)
                return PurchaseResult.Fail(ShopErrorCode.ItemNotFound, "mode");

            if (recipeId <= 0)
                return PurchaseResult.Fail(ShopErrorCode.CraftRecipeMismatch, "recipeId");

            if (entry.CraftRecipeIds == null || entry.CraftRecipeIds.Count == 0)
                return PurchaseResult.Fail(ShopErrorCode.CraftRecipeMismatch, "no craft for entry");

            var allowed = false;
            for (var i = 0; i < entry.CraftRecipeIds.Count; i++)
            {
                if (entry.CraftRecipeIds[i] == recipeId)
                {
                    allowed = true;
                    break;
                }
            }

            if (!allowed)
                return PurchaseResult.Fail(ShopErrorCode.CraftRecipeMismatch, "recipe not listed for entry");

            if (!CraftRecipeCatalog.TryGet(recipeId, out var rec) || rec == null)
                return PurchaseResult.Fail(ShopErrorCode.ItemNotFound, "recipe missing");

            if (rec.ResultItemConfigId != entry.ItemConfigId)
                return PurchaseResult.Fail(ShopErrorCode.CraftRecipeMismatch, "result mismatch");

            return CraftingService.TryCraft(hero, heroLevel, recipeId, in equipOptions);
        }
    }
}
