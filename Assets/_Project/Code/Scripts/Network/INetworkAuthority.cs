namespace Gameplay.Network
{
    public interface INetworkRoleReadOnly
    {
        bool HasServerAuthorityProcess { get; }
        bool IsClientOnlyProcess { get; }
    }

    public interface IAuthoritativeActionGate : INetworkRoleReadOnly
    {
        bool ShouldExecuteGameplayAuthoritativelyLocally { get; }
        bool CanSendGameplayIntentToServer { get; }
    }
}
