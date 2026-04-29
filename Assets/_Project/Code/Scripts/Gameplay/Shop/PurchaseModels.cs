using Core.Entity;

namespace Gameplay.Shop
{
    /// <summary> 商店 UI 获取某条 <see cref="ResolvedShopEntry"/> 的方式：<see cref="ShopAcquireMode.DirectGoldPurchase"/> 扣 <see cref="ItemConfigDefinition.BasePrice"/>；
    /// <see cref="CraftRecipe"/> 走 <see cref="CraftingService.TryCraft"/>（扣手续费+材料）。详见设计文档 §6.10。 </summary>
    public enum ShopAcquireMode
    {
        DirectGoldPurchase = 0,
        CraftRecipe = 1,
    }

    public struct PurchaseRequest
    {
        public EntityBase Hero;
        public int HeroLevel;
        public int ItemConfigId;
        public int PreferSlotIndex;
    }

    public struct PurchaseResult
    {
        public bool Success;
        public ShopErrorCode ErrorCode;
        public Equipment.EquipmentInstance Instance;
        public string DetailMessageOrEmpty;

        public static PurchaseResult Ok(Equipment.EquipmentInstance instance) =>
            new PurchaseResult
            {
                Success = true,
                ErrorCode = ShopErrorCode.None,
                Instance = instance,
                DetailMessageOrEmpty = null,
            };

        public static PurchaseResult Fail(ShopErrorCode code, string detail = null) =>
            new PurchaseResult
            {
                Success = false,
                ErrorCode = code,
                Instance = null,
                DetailMessageOrEmpty = detail,
            };
    }
}
