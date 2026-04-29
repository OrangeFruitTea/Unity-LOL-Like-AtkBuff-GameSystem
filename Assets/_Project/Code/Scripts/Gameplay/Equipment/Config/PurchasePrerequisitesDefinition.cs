using Newtonsoft.Json;

namespace Gameplay.Equipment.Config
{
    /// <summary> MVP：等级下限等；可按 JSON 增量扩展字段。 </summary>
    public sealed class PurchasePrerequisitesDefinition
    {
        [JsonProperty("minHeroLevel")]
        public int MinHeroLevel { get; set; }
    }
}
