using Basement.Events;
using Core.Entity;
using Gameplay.Equipment.Config;
using UnityEngine;

namespace Gameplay.Shop
{
    /// <summary> 静态合成配方：扣手续费 → 扣材料 → 发放成品（文档 §6.9）。 </summary>
    public static class CraftingService
    {
        public static PurchaseResult TryCraft(
            EntityBase hero,
            int heroLevel,
            int recipeId,
            in EquipmentEquipOptions equipOptions)
        {
            if (hero == null)
                return PurchaseResult.Fail(ShopErrorCode.ItemNotFound, "hero is null");

            if (!CraftRecipeCatalog.TryGet(recipeId, out var rec))
                return PurchaseResult.Fail(ShopErrorCode.ItemNotFound, $"recipe {recipeId}");

            if (!CraftRecipeCatalog.IsRecipeEnabled(rec))
                return PurchaseResult.Fail(ShopErrorCode.RecipeDisabled, $"recipe {recipeId} disabled");

            if (!EquipmentCatalog.TryGet(rec.ResultItemConfigId, out var resultDef))
                return PurchaseResult.Fail(ShopErrorCode.ItemNotFound, $"result {rec.ResultItemConfigId}");

            var loadout = HeroEquipmentLoadoutRegistry.GetOrCreate(hero);
            var mats = rec.Materials;
            if (mats == null || mats.Count == 0 || !EquipmentInventoryOperations.HasEnoughMaterials(loadout, mats))
                return PurchaseResult.Fail(ShopErrorCode.CraftInsufficientMaterials);

            if (!PurchaseService.Currency.TrySpend(rec.GoldCost))
                return PurchaseResult.Fail(ShopErrorCode.SpendFailed);

            if (!EquipmentInventoryOperations.TryConsumeWholeRecipeMaterials(hero, loadout, mats, equipOptions))
            {
                PurchaseService.Currency.TryRefund(rec.GoldCost);
                return PurchaseResult.Fail(ShopErrorCode.CraftConsumeFailed);
            }

            var delivered = PurchaseService.TryGrantPurchasedItem(hero, heroLevel, resultDef, equipOptions);
            if (!delivered.Success)
            {
                PurchaseService.Currency.TryRefund(rec.GoldCost);
                UnityEngine.Debug.LogError(
                    $"[CraftingService] Grant failed after mats consumed recipe={recipeId} err={delivered.ErrorCode} — 需回档材料（未实现原子事务）");
                return delivered;
            }

            PublishCraft(hero, rec, delivered.Instance);

            return delivered;
        }

        private static void PublishCraft(EntityBase hero, CraftRecipeDefinitionDto recipe, EquipmentInstance instance)
        {
            var bus = GameEventBus.Instance;
            if (bus == null)
                return;

            bus.Initialize();
            bus.Publish(new EquipmentCraftSucceededGameEvent
            {
                Hero = hero,
                RecipeId = recipe.RecipeId,
                ResultItemConfigId = recipe.ResultItemConfigId,
                InstanceOrNull = instance,
                SlotIndex = instance != null ? instance.SlotIndex : -1,
            });
        }
    }
}
