namespace Gameplay.Network
{
    /// <summary> 会话结束原因（领域事件载荷用）。 </summary>
    public enum NetworkSessionStopReason
    {
        Unknown,
        UserStopped,
        DisconnectFromRemote,
        TransportError,
    }
}
