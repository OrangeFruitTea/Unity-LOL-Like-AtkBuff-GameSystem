namespace Gameplay.Network
{
    public enum NetworkStartResult
    {
        Success,
        AlreadyRunning,
        InvalidParameters,
        TransportError,
        AbortedByUser,
    }
}
