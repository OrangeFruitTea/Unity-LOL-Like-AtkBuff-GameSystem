using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gameplay.Equipment.Config
{
    /// <summary>
    /// 单件装备上的一条 Buff 绑定（与技能 <c>BuffApplicationStepDefinition</c> 施加语义对齐的子集）。
    /// </summary>
    public sealed class EquipmentBuffBindingDefinition
    {
        [JsonProperty("bindingId")]
        public string BindingId { get; set; }

        [JsonProperty("buffId")]
        public int BuffId { get; set; }

        [JsonProperty("buffLevel")]
        public uint BuffLevel { get; set; } = 1;

        /// <summary> 装备强化等级每级额外叠加的 Buff 等级（与技能 levelScaling 类似）。 </summary>
        [JsonProperty("levelScalingPerItemTier")]
        public uint LevelScalingPerItemTier { get; set; }

        [JsonProperty("durationOverride")]
        public float? DurationOverride { get; set; }

        [JsonProperty("customArgs")]
        public List<object> CustomArgs { get; set; }
    }
}
