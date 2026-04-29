using System.Threading;
using Basement.Events;
using Core.Entity;
using Gameplay.Equipment;
using Gameplay.Equipment.Config;
using UnityEngine;

namespace Gameplay.Shop
{
    /// <summary>
    /// 购买与「仅发货」拆分：参见设计文档 §5.1、§7；合成在扣材料后调用 <see cref="TryGrantPurchasedItem"/>。
    /// </summary>
    public static class PurchaseService
    {
        private static long _nextInstanceId = 10_000;

        public static ICurrencyWallet Currency { get; set; } = new DebugCurrencyWallet(100_000);

        public static PurchaseResult TryPurchase(in PurchaseRequest request, in EquipmentEquipOptions equipOptions)
        {
            if (request.Hero == null)
                return PurchaseResult.Fail(ShopErrorCode.ItemNotFound, "hero is null");

            if (!EquipmentCatalog.TryGet(request.ItemConfigId, out var def))
                return PurchaseResult.Fail(ShopErrorCode.ItemNotFound, $"item {request.ItemConfigId}");

            var code = EquipmentRuleEngine.EvaluatePurchaseBaseline(def, request.HeroLevel, HeroEquipmentLoadoutRegistry.GetOrCreate(request.Hero));
            if (code != ShopErrorCode.None)
                return PurchaseResult.Fail(code);

            int price = def.BasePrice;
            if (price < 0)
                return PurchaseResult.Fail(ShopErrorCode.ItemNotFound, "invalid price");

            if (!Currency.TrySpend(price))
                return PurchaseResult.Fail(ShopErrorCode.SpendFailed);

            var deliver = DeliverPurchasedEquipment(request.Hero, request.HeroLevel, def, equipOptions);
            if (!deliver.Success)
                Currency.TryRefund(price);

            return deliver;
        }

        /// <summary>
        /// 在<strong>已通过</strong>经济与栏位语义校验、且外层已按需扣费时，写入栏位并按配置施加 Buff（直购或与合成配合）。
        /// </summary>
        public static PurchaseResult TryGrantPurchasedItem(
            EntityBase hero,
            int heroLevel,
            ItemConfigDefinition def,
            in EquipmentEquipOptions equipOptions) =>
            DeliverPurchasedEquipment(hero, heroLevel, def, equipOptions);

        private static PurchaseResult DeliverPurchasedEquipment(
            EntityBase hero,
            int heroLevel,
            ItemConfigDefinition def,
            in EquipmentEquipOptions equipOptions)
        {
            if (hero == null || def == null)
                return PurchaseResult.Fail(ShopErrorCode.ItemNotFound);

            var loadout = HeroEquipmentLoadoutRegistry.GetOrCreate(hero);

            var code = EquipmentRuleEngine.EvaluatePurchaseBaseline(def, heroLevel, loadout);
            if (code != ShopErrorCode.None)
                return PurchaseResult.Fail(code);

            if (EquipmentRuleEngine.TryMergeStack(def, loadout, 1, out var merged))
            {
                PublishSuccess(hero, def.ItemConfigId, merged, true);
                return PurchaseResult.Ok(merged);
            }

            int slot = loadout.FindFirstEmptySlotIndex();
            if (slot < 0)
                return PurchaseResult.Fail(ShopErrorCode.InventoryFull);

            long instId = Interlocked.Increment(ref _nextInstanceId);
            var inst = new EquipmentInstance(instId, def.ItemConfigId, hero, slot) { StackCount = 1 };

            if (!EquipmentBuffApplier.TryApplyEquippedBuffs(inst, equipOptions, null, out var buffErr))
            {
                Debug.LogWarning($"[PurchaseService] buff apply failed: {buffErr}");
                return PurchaseResult.Fail(ShopErrorCode.EquipBuffFailed, buffErr);
            }

            loadout.SetSlot(slot, inst);
            PublishEquipEvents(hero, inst);
            PublishSuccess(hero, def.ItemConfigId, inst, false);
            return PurchaseResult.Ok(inst);
        }

        /// <summary> 卸下并移除 Buff（出售/换装前）。 </summary>
        public static bool TryUnequipSlot(EntityBase hero, int slotIndex, in EquipmentEquipOptions options)
        {
            if (hero == null)
                return false;

            var loadout = HeroEquipmentLoadoutRegistry.GetOrCreate(hero);
            var inst = loadout.GetSlot(slotIndex);
            if (inst == null)
                return false;

            EquipmentBuffApplier.RemoveEquippedBuffs(inst);
            loadout.ClearSlot(slotIndex);
            inst.Owner = null;

            PublishUnequipped(hero, inst, slotIndex);
            return true;
        }

        private static void PublishSuccess(EntityBase hero, int cfgId, EquipmentInstance instance, bool wasStackMerge)
        {
            var bus = GameEventBus.Instance;
            if (bus == null)
                return;
            bus.Initialize();
            bus.Publish(new ShopPurchaseSucceededGameEvent
            {
                Hero = hero,
                ItemConfigId = cfgId,
                InstanceOrNull = instance,
                WasStackMerge = wasStackMerge,
            });
        }

        private static void PublishEquipEvents(EntityBase hero, EquipmentInstance inst)
        {
            var bus = GameEventBus.Instance;
            if (bus == null)
                return;

            bus.Initialize();
            bus.Publish(new EquipmentEquippedGameEvent
            {
                Hero = hero,
                Instance = inst,
                SlotIndex = inst.SlotIndex,
            });
        }

        private static void PublishUnequipped(EntityBase hero, EquipmentInstance inst, int slot)
        {
            var bus = GameEventBus.Instance;
            if (bus == null)
                return;

            bus.Initialize();
            bus.Publish(new EquipmentUnequippedGameEvent { Hero = hero, Instance = inst, SlotIndex = slot });
        }
    }
}
