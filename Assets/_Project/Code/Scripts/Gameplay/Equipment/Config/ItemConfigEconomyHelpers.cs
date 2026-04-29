using UnityEngine;

namespace Gameplay.Equipment.Config
{
    /// <summary> 出售退款比例等；与文档 §6.7 JSON 字段对齐。 </summary>
    public static class ItemConfigEconomyHelpers
    {
        public static bool CanSell(ItemConfigDefinition def) =>
            def != null && def.Sellable != false;

        /// <summary>
        /// 区间 (0,1]；省略或非法则 0.5。
        /// </summary>
        public static float GetSellRefundRatio(ItemConfigDefinition def)
        {
            if (def == null)
                return 0.5f;
            var r = def.SellRefundRatio;
            if (!r.HasValue || r.Value <= 0f || r.Value > 1f)
                return 0.5f;
            return r.Value;
        }

        public static int ComputeSellGold(ItemConfigDefinition def)
        {
            if (def == null)
                return 0;
            return Mathf.FloorToInt(def.BasePrice * GetSellRefundRatio(def));
        }
    }
}
