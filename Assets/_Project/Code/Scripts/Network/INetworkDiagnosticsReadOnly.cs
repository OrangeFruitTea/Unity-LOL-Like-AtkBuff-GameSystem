namespace Gameplay.Network
{
    public interface INetworkDiagnosticsReadOnly
    {
        float SmoothedRttSecondsOrNegative { get; }
        ulong TransportMessagesOutgoing { get; }
        ulong TransportMessagesIncoming { get; }
    }
}
