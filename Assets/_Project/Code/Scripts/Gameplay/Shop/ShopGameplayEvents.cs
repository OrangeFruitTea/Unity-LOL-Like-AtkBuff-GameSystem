using System;
using Basement.Events;
using Core.Entity;
using Gameplay.Equipment;

namespace Gameplay.Shop
{
    public sealed class ShopPurchaseSucceededGameEvent : IGameEvent
    {
        public string EventId => nameof(ShopPurchaseSucceededGameEvent);

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public GameEventPriority Priority => GameEventPriority.Normal;

        public EntityBase Hero { get; set; }

        public int ItemConfigId { get; set; }

        public EquipmentInstance InstanceOrNull { get; set; }

        public bool WasStackMerge { get; set; }
    }

    public sealed class ShopPurchaseFailedGameEvent : IGameEvent
    {
        public string EventId => nameof(ShopPurchaseFailedGameEvent);

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public GameEventPriority Priority => GameEventPriority.Normal;

        public EntityBase Hero { get; set; }

        public int ItemConfigId { get; set; }

        public ShopErrorCode Reason { get; set; }
    }

    public sealed class EquipmentSoldGameEvent : IGameEvent
    {
        public string EventId => nameof(EquipmentSoldGameEvent);

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public GameEventPriority Priority => GameEventPriority.Normal;

        public EntityBase Hero { get; set; }

        public int ItemConfigId { get; set; }

        public int GoldEarned { get; set; }

        public int PreviousSlotIndex { get; set; }

        public bool WasSold { get; set; }
    }

    public sealed class EquipmentEquippedGameEvent : IGameEvent
    {
        public string EventId => nameof(EquipmentEquippedGameEvent);

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public GameEventPriority Priority => GameEventPriority.Normal;

        public EntityBase Hero { get; set; }

        public EquipmentInstance Instance { get; set; }

        public int SlotIndex { get; set; }
    }

    public sealed class EquipmentUnequippedGameEvent : IGameEvent
    {
        public string EventId => nameof(EquipmentUnequippedGameEvent);

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public GameEventPriority Priority => GameEventPriority.Normal;

        public EntityBase Hero { get; set; }

        public EquipmentInstance Instance { get; set; }

        public int SlotIndex { get; set; }
    }

    public sealed class EquipmentCraftSucceededGameEvent : IGameEvent
    {
        public string EventId => nameof(EquipmentCraftSucceededGameEvent);

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public GameEventPriority Priority => GameEventPriority.Normal;

        public EntityBase Hero { get; set; }

        public int RecipeId { get; set; }

        public int ResultItemConfigId { get; set; }

        public EquipmentInstance InstanceOrNull { get; set; }

        public int SlotIndex { get; set; }
    }
}
