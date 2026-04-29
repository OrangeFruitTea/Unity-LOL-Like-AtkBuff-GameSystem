using Gameplay.Equipment.Config;
using UnityEngine;

namespace Gameplay.Shop
{
    /// <summary>
    /// 装备层规则：唯一、唯一被动组、堆叠；设计文档 §5.2 / §6.5。
    /// </summary>
    public static class EquipmentRuleEngine
    {
        public static bool ContainsUniqueItemConflict(ItemConfigDefinition newItem, HeroEquipmentLoadout loadout)
        {
            if (newItem == null || loadout == null)
                return false;
            if (!newItem.UniqueItem)
                return false;
            foreach (var inst in loadout.EnumerateOccupied())
            {
                if (inst.ItemConfigId == newItem.ItemConfigId)
                    return true;
            }

            return false;
        }

        public static bool ContainsUniqueGroupConflict(ItemConfigDefinition newItem, HeroEquipmentLoadout loadout)
        {
            if (newItem == null || loadout == null)
                return false;
            if (string.IsNullOrEmpty(newItem.UniqueGroupId))
                return false;
            foreach (var inst in loadout.EnumerateOccupied())
            {
                if (!EquipmentCatalog.TryGet(inst.ItemConfigId, out var existing))
                    continue;
                if (!string.IsNullOrEmpty(existing.UniqueGroupId) &&
                    existing.UniqueGroupId == newItem.UniqueGroupId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 尝试将 <paramref name="amount"/> 件商品叠入已有格；成功则 stackOut 为合并后实例。
        /// </summary>
        public static bool TryMergeStack(ItemConfigDefinition def, HeroEquipmentLoadout loadout, int amount, out Equipment.EquipmentInstance stackOut)
        {
            stackOut = null;
            if (def == null || def.MaxStack <= 1 || loadout == null)
                return false;

            if (def.EquippedBuffs != null && def.EquippedBuffs.Count > 0)
                return false;

            foreach (var inst in loadout.EnumerateOccupied())
            {
                if (inst.ItemConfigId != def.ItemConfigId)
                    continue;
                long room = def.MaxStack - inst.StackCount;
                if (room <= 0)
                    continue;

                int add = Mathf.Min(amount, (int)room);
                inst.StackCount += add;
                stackOut = inst;
                return true;
            }

            return false;
        }

        public static ShopErrorCode EvaluatePurchaseBaseline(ItemConfigDefinition def, int heroLevel, HeroEquipmentLoadout loadout)
        {
            if (def == null)
                return ShopErrorCode.ItemNotFound;
            if (def.Purchasable == false)
                return ShopErrorCode.NotPurchasable;

            var pre = def.PurchasePrerequisites;
            if (pre != null && heroLevel < pre.MinHeroLevel)
                return ShopErrorCode.PrerequisiteNotMet;

            if (ContainsUniqueItemConflict(def, loadout))
                return ShopErrorCode.UniqueConflict;

            if (ContainsUniqueGroupConflict(def, loadout))
                return ShopErrorCode.UniqueConflict;

            if (!HasRoomFor(def, loadout))
                return ShopErrorCode.InventoryFull;

            return ShopErrorCode.None;
        }

        public static bool HasRoomFor(ItemConfigDefinition def, HeroEquipmentLoadout loadout)
        {
            if (def == null || loadout == null)
                return false;

            if (def.EquippedBuffs != null && def.EquippedBuffs.Count > 0)
                return loadout.FindFirstEmptySlotIndex() >= 0;

            if (def.MaxStack > 1)
            {
                foreach (var inst in loadout.EnumerateOccupied())
                {
                    if (inst.ItemConfigId != def.ItemConfigId)
                        continue;
                    if (inst.StackCount < def.MaxStack)
                        return true;
                }
            }

            return loadout.FindFirstEmptySlotIndex() >= 0;
        }
    }
}
