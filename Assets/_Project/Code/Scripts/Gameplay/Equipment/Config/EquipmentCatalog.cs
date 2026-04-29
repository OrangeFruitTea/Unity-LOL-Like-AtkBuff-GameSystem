using System;
using System.Collections.Generic;

namespace Gameplay.Equipment.Config
{
    /// <summary>
    /// 装备定义只读目录（由 <see cref="Loading.EquipmentDataLoader"/> 或测试填充）。
    /// </summary>
    public static class EquipmentCatalog
    {
        private static readonly Dictionary<int, ItemConfigDefinition> ById = new Dictionary<int, ItemConfigDefinition>();

        public static void Clear() => ById.Clear();

        public static void Register(ItemConfigDefinition def)
        {
            if (def == null)
                return;
            ById[def.ItemConfigId] = def;
        }

        public static void ReplaceAll(IEnumerable<ItemConfigDefinition> definitions)
        {
            ById.Clear();
            if (definitions == null)
                return;
            foreach (var d in definitions)
            {
                if (d != null)
                    ById[d.ItemConfigId] = d;
            }
        }

        public static bool TryGet(int itemConfigId, out ItemConfigDefinition definition) =>
            ById.TryGetValue(itemConfigId, out definition);

        public static IReadOnlyDictionary<int, ItemConfigDefinition> All => ById;

        /// <summary>
        /// 加载后校验：重复 bindingId、Buff 表、注册表（日志警告，不抛异常）。
        /// </summary>
        public static void ValidateLoadedData(bool checkBuffData, bool checkRegistry)
        {
            foreach (var kv in ById)
            {
                var item = kv.Value;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                if (item.EquippedBuffs == null)
                    continue;
                foreach (var b in item.EquippedBuffs)
                {
                    if (b == null || string.IsNullOrEmpty(b.BindingId))
                    {
                        UnityEngine.Debug.LogWarning($"[EquipmentCatalog] item {item.ItemConfigId} 存在空 binding");
                        continue;
                    }

                    if (!seen.Add(b.BindingId))
                        UnityEngine.Debug.LogWarning($"[EquipmentCatalog] item {item.ItemConfigId} 重复 bindingId={b.BindingId}");

                    if (checkBuffData && BuffDataLoader.Instance != null &&
                        !BuffDataLoader.Instance.TryGet(b.BuffId, out _))
                        UnityEngine.Debug.LogWarning($"[EquipmentCatalog] item {item.ItemConfigId} binding {b.BindingId} 引用缺失的 BuffData id={b.BuffId}");

                    if (checkRegistry && !Gameplay.Skill.Buff.BuffTypeRegistry.TryGetFactory(b.BuffId, out _))
                        UnityEngine.Debug.LogWarning($"[EquipmentCatalog] item {item.ItemConfigId} binding {b.BindingId} 无 BuffTypeRegistry 工厂 buffId={b.BuffId}");
                }
            }
        }
    }
}
