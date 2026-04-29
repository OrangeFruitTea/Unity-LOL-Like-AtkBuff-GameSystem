using System;
using System.Collections.Generic;
using System.Linq;
using Core.Entity;
using Gameplay.Equipment;
using Gameplay.Equipment.Config;
using UnityEngine;

namespace Gameplay.Shop
{
    /// <summary>
    /// 单英雄装备栏位（局内）；设计文档 HeroLoadout / FR-INV。
    /// </summary>
    public sealed class HeroEquipmentLoadout
    {
        private readonly EquipmentInstance[] _slots;

        public HeroEquipmentLoadout(EntityBase hero, int slotCount)
        {
            Hero = hero;
            if (slotCount < 1)
                slotCount = 1;
            _slots = new EquipmentInstance[slotCount];
        }

        public EntityBase Hero { get; }

        public int SlotCount => _slots.Length;

        public EquipmentInstance GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Length)
                return null;
            return _slots[index];
        }

        public IEnumerable<EquipmentInstance> EnumerateOccupied() =>
            _slots.Where(s => s != null);

        public int FindFirstEmptySlotIndex()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                    return i;
            }

            return -1;
        }

        public void SetSlot(int index, EquipmentInstance instance)
        {
            if (index < 0 || index >= _slots.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            _slots[index] = instance;
            if (instance != null)
                instance.SlotIndex = index;
        }

        public void ClearSlot(int index)
        {
            if (index < 0 || index >= _slots.Length)
                return;
            _slots[index] = null;
        }
    }

    /// <summary> 局内多英雄栏位登记（单机 / 权威端持有）。 </summary>
    public static class HeroEquipmentLoadoutRegistry
    {
        private static int _defaultSlotCount = 6;

        private static readonly Dictionary<EntityBase, HeroEquipmentLoadout> ByHero =
            new Dictionary<EntityBase, HeroEquipmentLoadout>();

        public static void ConfigureDefaultSlotCount(int count)
        {
            if (count > 0)
                _defaultSlotCount = count;
        }

        public static HeroEquipmentLoadout GetOrCreate(EntityBase hero)
        {
            if (hero == null)
                return null;
            if (ByHero.TryGetValue(hero, out var loadout))
                return loadout;

            loadout = new HeroEquipmentLoadout(hero, _defaultSlotCount);
            ByHero[hero] = loadout;
            return loadout;
        }

        public static bool TryRemoveHero(EntityBase hero, out HeroEquipmentLoadout removed)
        {
            removed = null;
            if (hero == null || !ByHero.TryGetValue(hero, out removed))
                return false;
            ByHero.Remove(hero);
            return true;
        }

        public static void ClearAll()
        {
            ByHero.Clear();
        }
    }
}
