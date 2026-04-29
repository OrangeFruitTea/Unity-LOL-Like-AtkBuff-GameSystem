namespace Gameplay.Shop
{
    public enum ShopErrorCode
    {
        None = 0,
        ItemNotFound,
        NotPurchasable,
        InsufficientCurrency,
        InventoryFull,
        UniqueConflict,
        PrerequisiteNotMet,
        SpendFailed,
        EquipBuffFailed,
        SellNotAllowed,
        RecipeDisabled,
        CraftInsufficientMaterials,
        CraftConsumeFailed,
        /// <summary> 商店「按配方合成」所选 recipe 与当前条目或成品 id 不一致。 </summary>
        CraftRecipeMismatch,
    }

    public enum EquipBuffErrorCode
    {
        None = 0,
        BuffDataMissing,
        BuffNotRegistered,
        ApplyFailed,
        RemoveFailed,
        StrictRollback,
    }
}
