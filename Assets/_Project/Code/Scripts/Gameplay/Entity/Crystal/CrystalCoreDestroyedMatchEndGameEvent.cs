using System;
using Basement.Events;

namespace Gameplay.Entity
{
    /// <summary>
    /// 水晶被推倒后的对局终局语义事件（单机 Demo）；供未来 <c>MatchFlow</c> 订阅进入 Settling。<br/>
    /// 触发源：<see cref="CrystalMatchOutcomeBridge"/>。
    /// </summary>
    public sealed class CrystalCoreDestroyedMatchEndGameEvent : IGameEvent
    {
        public string EventId => nameof(CrystalCoreDestroyedMatchEndGameEvent);

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public GameEventPriority Priority => GameEventPriority.High;

        /// <summary>战败方阵营（水晶所有者）。</summary>
        public byte LosingTeamId { get; set; }

        /// <summary>胜利方阵营。</summary>
        public byte WinningTeamId { get; set; }

        public long CrystalEntityId { get; set; }

        public long KillerEntityId { get; set; }
    }
}
