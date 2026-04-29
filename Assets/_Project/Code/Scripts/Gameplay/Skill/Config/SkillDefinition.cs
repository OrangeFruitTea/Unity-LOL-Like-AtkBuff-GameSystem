using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gameplay.Skill.Config
{
    /// <summary>
    /// 技能静态定义：元数据 + Buff 施加管线。
    /// </summary>
    public sealed class SkillDefinition
    {
        [JsonProperty("skillId")]
        public int SkillId { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("maxLevel")]
        public int MaxLevel { get; set; } = 1;

        [JsonProperty("cooldownSeconds")]
        public float CooldownSeconds { get; set; }

        [JsonProperty("requiresTarget")]
        public bool RequiresTarget { get; set; }

        [JsonProperty("castRange")]
        public float CastRange { get; set; }

        [JsonProperty("steps")]
        public List<BuffApplicationStepDefinition> Steps { get; set; } = new List<BuffApplicationStepDefinition>();
    }
}
