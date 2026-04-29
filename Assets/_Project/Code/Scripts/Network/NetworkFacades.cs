namespace Gameplay.Network
{
    /// <summary> UI / Gameplay 单点入口；单机测试可替换为 stub。 </summary>
    public static class NetworkFacades
    {
        public static INetworkSessionReadOnly Session { get; set; }
        public static INetworkSessionControl SessionControl => Session as INetworkSessionControl;

        public static IAuthoritativeActionGate Authority { get; set; }

        public static ILanLobbyBrowser Lobby { get; set; }

        public static INetworkConfigProvider NetConfig { get; set; }

        /// <summary> 可为 null；HUD 使用前判空。 </summary>
        public static INetworkDiagnosticsReadOnly Diagnostics { get; set; }
    }
}
