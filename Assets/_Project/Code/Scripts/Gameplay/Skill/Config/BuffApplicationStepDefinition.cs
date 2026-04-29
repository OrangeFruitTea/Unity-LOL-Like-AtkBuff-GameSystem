using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gameplay.Skill.Config
{
    /// <summary>
    /// 单步 Buff 施加描述（可序列化）；运行时由 <see cref="SkillDefinition"/> 持有。
    /// </summary>
    public sealed class BuffApplicationStepDefinition
    {
        [JsonProperty("stepId")]
        public string StepId { get; set; }

        [JsonProperty("buffId")]
        public int BuffId { get; set; }

        [JsonProperty("targetSelector")]
        public SkillTargetSelectorKind TargetSelector { get; set; }

        [JsonProperty("buffLevel")]
        public uint BuffLevel { get; set; } = 1;

        /// <summary> 每级技能额外增加的 Buff 等级。 </summary>
        [JsonProperty("levelScalingPerSkillLevel")]
        public uint LevelScalingPerSkillLevel { get; set; }

        [JsonProperty("durationOverride")]
        public float? DurationOverride { get; set; }

        [JsonProperty("customArgs")]
        public List<object> CustomArgs { get; set; }

        [JsonProperty("triggerKind")]
        public BuffApplicationTriggerKind TriggerKind { get; set; } = BuffApplicationTriggerKind.Immediate;

        /// <summary> 相对施法起点的延迟秒数（<see cref="BuffApplicationTriggerKind.AfterDelay"/>）。 </summary>
        [JsonProperty("delaySecondsFromCastStart")]
        public float DelaySecondsFromCastStart { get; set; }

        [JsonProperty("parallelGroup")]
        public int ParallelGroup { get; set; }

        [JsonProperty("conditionId")]
        public string ConditionId { get; set; }

        [JsonProperty("conditionParam")]
        public float ConditionParam { get; set; }

        [JsonProperty("eventId")]
        public string EventId { get; set; }
    }
}
