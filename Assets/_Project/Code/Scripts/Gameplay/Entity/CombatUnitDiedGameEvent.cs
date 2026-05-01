using System;
using Basement.Events;

namespace Gameplay.Entity
{
    /// <summary>
    /// 局内单位首次判定击倒（Hp≤阈）且 <see cref="Core.Entity.UnitVitalitySystem"/> 已完成击杀者回填后派发。<br/>
    /// UI / 音效 / MVP 回放等订阅 <see cref="Basement.Events.GameEventBus"/>；与 <see cref="Core.Entity.UnitDeathEventHub"/>（C# event）并行。
    /// </summary>
    public sealed class CombatUnitDiedGameEvent : IGameEvent
    {
        public string EventId => nameof(CombatUnitDiedGameEvent);

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public GameEventPriority Priority => GameEventPriority.High;

        /// <summary>死亡单位 ECS Id。</summary>
        public long VictimEntityId { get; set; }

        /// <summary>击杀者 ECS Id（无主手/未知时可能为 0）。</summary>
        public long KillerEntityId { get; set; }
    }
}
