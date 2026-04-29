using System.Collections.Generic;
using Core.Entity;
using Gameplay.Equipment;
using Gameplay.Equipment.Config;
using UnityEngine;

namespace Gameplay.Shop
{
    /// <summary>
    /// 仅针对局内装载栏的材料扣减（堆叠减值 / 清仓卸 Buff）；§5.3、合成消耗。
    /// </summary>
    public static class EquipmentInventoryOperations
    {
        public static int CountItemUnits(HeroEquipmentLoadout loadout, int itemConfigId)
        {
            if (loadout == null || itemConfigId <= 0)
                return 0;

            int sum = 0;
            for (int i = 0; i < loadout.SlotCount; i++)
            {
                var inst = loadout.GetSlot(i);
                if (inst?.ItemConfigId == itemConfigId)
                    sum += Mathf.Max(1, inst.StackCount);
            }

            return sum;
        }

        public static bool HasEnoughMaterials(HeroEquipmentLoadout loadout, IReadOnlyList<CraftMaterialEntryDto> mats)
        {
            if (loadout == null || mats == null)
                return false;
            foreach (var m in mats)
            {
                if (m == null || m.Count <= 0)
                    continue;

                if (CountItemUnits(loadout, m.ItemConfigId) < m.Count)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 从栏位扣除指定件数；多件同 id 可多格凑齐；扣至 0 时卸 Buff。
        /// </summary>
        public static bool TryConsumeUnits(
            HeroEquipmentLoadout loadout,
            int itemConfigId,
            int quantityToRemove,
            in EquipmentEquipOptions equipOptions)
        {
            if (loadout == null || itemConfigId <= 0 || quantityToRemove <= 0)
                return false;

            var remaining = quantityToRemove;

            while (remaining > 0)
            {
                var progressed = false;
                for (int i = 0; i < loadout.SlotCount && remaining > 0; i++)
                {
                    var inst = loadout.GetSlot(i);
                    if (inst?.ItemConfigId != itemConfigId)
                        continue;

                    int stackUnits = Mathf.Max(1, inst.StackCount);
                    int take = Mathf.Min(remaining, stackUnits);

                    if (take >= stackUnits)
                    {
                        EquipmentBuffApplier.RemoveEquippedBuffs(inst);
                        loadout.ClearSlot(i);
                        inst.Owner = null;
                        remaining -= stackUnits;
                    }
                    else
                    {
                        inst.StackCount = stackUnits - take;
                        remaining -= take;
                    }

                    progressed = true;
                    break;
                }

                if (!progressed)
                    return false;
            }

            return true;
        }

        public static bool TryConsumeWholeRecipeMaterials(
            EntityBase hero,
            HeroEquipmentLoadout loadout,
            IReadOnlyList<CraftMaterialEntryDto> materials,
            in EquipmentEquipOptions equipOptions)
        {
            if (hero == null || loadout == null || materials == null)
                return false;

            if (!HasEnoughMaterials(loadout, materials))
                return false;

            foreach (var m in materials)
            {
                if (m == null || m.Count <= 0)
                    continue;

                if (!TryConsumeUnits(loadout, m.ItemConfigId, m.Count, equipOptions))
                {
                    Debug.LogError($"[EquipmentInventoryOperations] 扣减失败 item={m.ItemConfigId}");
                    return false;
                }
            }

            return true;
        }
    }
}
