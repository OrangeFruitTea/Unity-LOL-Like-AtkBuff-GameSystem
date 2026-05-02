using Basement.Events;
using Core.Entity;
using Gameplay.Equipment;
using Gameplay.Equipment.Config;
using UnityEngine;

namespace Gameplay.Shop
{
    /// <summary> 卖出栏位物品：等价于卸下 + 按配置的退款比例入账（§6.7）。 </summary>
    public static class EquipmentSellService
    {
        public struct SellEquipmentResult
        {
            public bool Success;
            public ShopErrorCode Code;
            public int GoldEarned;
            public string MessageOrEmpty;
        }

        public static SellEquipmentResult TrySellEquippedSlot(EntityBase hero, int slotIndex, in EquipmentEquipOptions unequipOptions)
        {
            if (hero == null)
                return Fail(ShopErrorCode.ItemNotFound, "hero null");

            var loadout = HeroEquipmentLoadoutRegistry.GetOrCreate(hero);
            var inst = loadout.GetSlot(slotIndex);
            if (inst == null)
                return Fail(ShopErrorCode.ItemNotFound, "empty slot");

            if (!EquipmentCatalog.TryGet(inst.ItemConfigId, out var def))
                return Fail(ShopErrorCode.ItemNotFound, "cfg missing");

            if (!ItemConfigEconomyHelpers.CanSell(def))
                return Fail(ShopErrorCode.SellNotAllowed, $"item {def.ItemConfigId} not sellable");

            int gold = ItemConfigEconomyHelpers.ComputeSellGold(def);
            if (gold < 0)
                gold = 0;

            if (!PurchaseService.TryUnequipSlot(hero, slotIndex, unequipOptions))
                return Fail(ShopErrorCode.ItemNotFound, "unequip failed");

            if (gold > 0 && !PurchaseService.Currency.TryRefund(gold))
                Debug.LogWarning($"[EquipmentSellService] 金币入账失败 gold={gold}");

            PublishSold(hero, def.ItemConfigId, gold, slotIndex);

            return new SellEquipmentResult
            {
                Success = true,
                Code = ShopErrorCode.None,
                GoldEarned = gold,
                MessageOrEmpty = null,
            };
        }

        private static SellEquipmentResult Fail(ShopErrorCode code, string msg) =>
            new SellEquipmentResult { Success = false, Code = code, GoldEarned = 0, MessageOrEmpty = msg };

        private static void PublishSold(EntityBase hero, int itemConfigId, int gold, int previousSlot)
        {
            var bus = GameEventBus.Instance;
            if (bus == null)
                return;

            bus.Initialize();
            bus.Publish(new EquipmentSoldGameEvent
            {
                Hero = hero,
                ItemConfigId = itemConfigId,
                GoldEarned = gold,
                PreviousSlotIndex = previousSlot,
                WasSold = true,
            });
        }
    }
}
