using System.Collections.Generic;
using Core.Entity;

namespace Gameplay.Equipment
{
    /// <summary>
    /// 局内一件装备实例：配置引用 + 持有者 + 运行时 <c>bindingId → BuffBase</c>（供卸下时移除）。
    /// </summary>
    public sealed class EquipmentInstance
    {
        public EquipmentInstance(long instanceId, int itemConfigId, EntityBase owner, int slotIndex = -1)
        {
            InstanceId = instanceId;
            ItemConfigId = itemConfigId;
            Owner = owner;
            SlotIndex = slotIndex;
        }

        public long InstanceId { get; }

        public int ItemConfigId { get; }

        public EntityBase Owner { get; set; }

        public int SlotIndex { get; set; }

        /// <summary>可堆叠消耗品用；大件默认 1。 </summary>
        public int StackCount { get; set; } = 1;

        private readonly Dictionary<string, BuffBase> _buffByBinding = new Dictionary<string, BuffBase>();

        internal IReadOnlyDictionary<string, BuffBase> BuffByBindingForDebug => _buffByBinding;

        internal void RegisterAppliedBuff(string bindingId, BuffBase buff)
        {
            if (string.IsNullOrEmpty(bindingId) || buff == null)
                return;
            _buffByBinding[bindingId] = buff;
        }

        internal void ClearAppliedBuffMap() => _buffByBinding.Clear();

        internal bool TryGetAppliedBuff(string bindingId, out BuffBase buff) =>
            _buffByBinding.TryGetValue(bindingId, out buff);
    }
}
