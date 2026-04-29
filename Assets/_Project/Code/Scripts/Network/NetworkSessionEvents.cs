using System;
using Basement.Events;

namespace Gameplay.Network.Events
{
    public sealed class NetworkSessionStartedEvent : IGameEvent
    {
        public string EventId => nameof(NetworkSessionStartedEvent);
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public GameEventPriority Priority => GameEventPriority.Normal;

        public NetworkRunMode Mode { get; set; }
    }

    public sealed class NetworkSessionStoppingEvent : IGameEvent
    {
        public string EventId => nameof(NetworkSessionStoppingEvent);
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public GameEventPriority Priority => GameEventPriority.High;

        public NetworkSessionStopReason Reason { get; set; }
    }

    public sealed class NetworkSessionStoppedEvent : IGameEvent
    {
        public string EventId => nameof(NetworkSessionStoppedEvent);
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public GameEventPriority Priority => GameEventPriority.Normal;

        public NetworkSessionStopReason Reason { get; set; }
    }

    public sealed class NetworkPlayerTopologyChangedEvent : IGameEvent
    {
        public string EventId => nameof(NetworkPlayerTopologyChangedEvent);
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public GameEventPriority Priority => GameEventPriority.Normal;

        public int ConnectionsWithPlayer { get; set; }
    }

    public sealed class NetworkGameplaySceneChangedEvent : IGameEvent
    {
        public string EventId => nameof(NetworkGameplaySceneChangedEvent);
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public GameEventPriority Priority => GameEventPriority.Normal;

        /// <summary> Mirror 侧的 networkSceneName 或场景名片段。 </summary>
        public string SceneNameOrPath { get; set; }
    }
}
