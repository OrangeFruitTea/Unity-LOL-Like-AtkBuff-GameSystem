using System;
using Basement.Events;

namespace Gameplay.Entity
{
    /// <summary>
    /// 单位软复活完成（传送回出生点、HP 回满、黑板复位）后派发；与 <see cref="CombatUnitDiedGameEvent"/> 配对。
    /// </summary>
    public sealed class CombatUnitRespawnedGameEvent : IGameEvent
    {
        public string EventId => nameof(CombatUnitRespawnedGameEvent);

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public GameEventPriority Priority => GameEventPriority.Normal;

        public long UnitEntityId { get; set; }
    }
}
