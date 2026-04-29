using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gameplay.Skill.Config
{
    /// <summary>
    /// StreamingAssets 技能表根对象。
    /// </summary>
    public sealed class SkillDataFileDto
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [JsonProperty("skills")]
        public List<SkillDefinition> Skills { get; set; } = new List<SkillDefinition>();
    }
}
