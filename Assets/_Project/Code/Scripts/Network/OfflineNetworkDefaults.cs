namespace Gameplay.Network
{
    /// <summary> 单机/无 Mirror 场景的默认 façade，便于离线跑 Gameplay。 </summary>
    public static class OfflineNetworkDefaults
    {
        private static readonly OfflineSessionReadOnly OfflineSession = new OfflineSessionReadOnly();
        private static readonly OfflineGameplayAuthorityGate OfflineAuthority = new OfflineGameplayAuthorityGate();

        /// <summary> 赋值 <see cref="NetworkFacades"/> 为离线语义（可重复调用）。 </summary>
        public static void Apply()
        {
            NetworkFacades.Session = OfflineSession;
            NetworkFacades.Authority = OfflineAuthority;
            NetworkFacades.Lobby ??= new NullLanLobbyBrowser();
            NetworkFacades.Diagnostics = null;
        }

        private sealed class OfflineSessionReadOnly : INetworkSessionReadOnly
        {
            public bool IsHostRunning => false;
            public bool IsServerActive => false;
            public bool IsClientConnected => false;
            public bool IsOnlineSceneLoaded => false;
            public ushort ActivePort => 0;
            public string LastRemoteAddressOrEmpty => string.Empty;
        }

        private sealed class OfflineGameplayAuthorityGate : IAuthoritativeActionGate
        {
            public bool HasServerAuthorityProcess => true;
            public bool IsClientOnlyProcess => false;
            public bool ShouldExecuteGameplayAuthoritativelyLocally => true;
            public bool CanSendGameplayIntentToServer => false;
        }
    }
}
