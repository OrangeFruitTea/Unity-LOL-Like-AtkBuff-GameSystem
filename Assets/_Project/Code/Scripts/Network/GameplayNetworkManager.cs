using Basement.Events;
using Gameplay.Network.Events;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay.Network
{
    /// <summary>
    /// Mirror <see cref="NetworkManager"/> 的薄适配：集中实现 <see cref="INetworkSessionControl"/>、<see cref="IAuthoritativeActionGate"/>、
    /// <see cref="INetworkDiagnosticsReadOnly"/>，并在 <see cref="Awake"/> 中注册至 <see cref="NetworkFacades"/>。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Gameplay/Gameplay Network Manager")]
    public sealed class GameplayNetworkManager : NetworkManager,
        INetworkSessionControl,
        IAuthoritativeActionGate,
        INetworkDiagnosticsReadOnly
    {
        public static new GameplayNetworkManager singleton => (GameplayNetworkManager)NetworkManager.singleton;

        private string _lastRemoteAddressOrEmpty = "";

        public bool IsHostRunning => mode == NetworkManagerMode.Host;

        public bool IsServerActive => NetworkServer.active;

        public bool IsClientConnected => NetworkClient.isConnected;

        public bool IsOnlineSceneLoaded =>
            isNetworkActive &&
            !string.IsNullOrEmpty(networkSceneName) &&
            SceneManager.GetActiveScene().path == networkSceneName;

        public ushort ActivePort =>
            transport is PortTransport pt ? pt.Port : (ushort)0;

        public string LastRemoteAddressOrEmpty => _lastRemoteAddressOrEmpty;

        bool INetworkRoleReadOnly.HasServerAuthorityProcess => NetworkServer.active;

        bool INetworkRoleReadOnly.IsClientOnlyProcess => NetworkClient.active && !NetworkServer.active;

        public bool ShouldExecuteGameplayAuthoritativelyLocally => NetworkServer.active;

        public bool CanSendGameplayIntentToServer => NetworkClient.active && !NetworkServer.active;

        public float SmoothedRttSecondsOrNegative =>
            NetworkClient.isConnected ? (float)NetworkTime.rtt : -1f;

        public ulong TransportMessagesOutgoing => 0;

        public ulong TransportMessagesIncoming => 0;

        public override void Awake()
        {
            base.Awake();
            GameEventBus.Instance?.Initialize();

            NetworkFacades.Session = this;
            NetworkFacades.Authority = this;
            NetworkFacades.Diagnostics = this;

            Application.runInBackground = runInBackground;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(NetworkFacades.Session, this))
                NetworkFacades.Session = null;
            if (ReferenceEquals(NetworkFacades.Authority, this))
                NetworkFacades.Authority = null;
            if (ReferenceEquals(NetworkFacades.Diagnostics, this))
                NetworkFacades.Diagnostics = null;
        }

        public NetworkStartResult TryStartHost(ushort listenPort)
        {
            if (!Application.isPlaying)
                return NetworkStartResult.AbortedByUser;
            if (NetworkServer.active || NetworkClient.active)
                return NetworkStartResult.AlreadyRunning;
            if (listenPort == 0)
                return NetworkStartResult.InvalidParameters;

            ApplyListenPort(listenPort);
            try
            {
                StartHost();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                return NetworkStartResult.TransportError;
            }

            return NetworkStartResult.Success;
        }

        public NetworkStartResult TryStartServer(ushort listenPort)
        {
            if (!Application.isPlaying)
                return NetworkStartResult.AbortedByUser;
            if (NetworkServer.active || NetworkClient.active)
                return NetworkStartResult.AlreadyRunning;
            if (listenPort == 0)
                return NetworkStartResult.InvalidParameters;

            ApplyListenPort(listenPort);
            try
            {
                StartServer();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                return NetworkStartResult.TransportError;
            }

            return NetworkStartResult.Success;
        }

        public NetworkStartResult TryStartClient(string host, ushort port)
        {
            if (!Application.isPlaying)
                return NetworkStartResult.AbortedByUser;
            if (NetworkClient.active || NetworkServer.active)
                return NetworkStartResult.AlreadyRunning;

            ApplyListenPort(port);
            networkAddress = string.IsNullOrWhiteSpace(host) ? "localhost" : host.Trim();
            _lastRemoteAddressOrEmpty = networkAddress;

            try
            {
                StartClient();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                return NetworkStartResult.TransportError;
            }

            return NetworkStartResult.Success;
        }

        public void StopAll()
        {
            if (!NetworkServer.active && !NetworkClient.active)
                return;

            SafePublish(new NetworkSessionStoppingEvent { Reason = NetworkSessionStopReason.UserStopped });

            if (NetworkServer.active && NetworkClient.active)
                StopHost();
            else if (NetworkServer.active)
                StopServer();
            else if (NetworkClient.active)
                StopClient();

            SafePublish(new NetworkSessionStoppedEvent { Reason = NetworkSessionStopReason.UserStopped });
        }

        public bool ServerChangeGameplayScene(string sceneName)
        {
            if (!NetworkServer.active || string.IsNullOrEmpty(sceneName))
                return false;
            ServerChangeScene(sceneName);
            return true;
        }

        public override void OnStartHost()
        {
            base.OnStartHost();
            SafePublish(new NetworkSessionStartedEvent { Mode = NetworkRunMode.Host });
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (mode == NetworkManagerMode.ServerOnly)
                SafePublish(new NetworkSessionStartedEvent { Mode = NetworkRunMode.DedicatedServer });
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (mode == NetworkManagerMode.ClientOnly)
                SafePublish(new NetworkSessionStartedEvent { Mode = NetworkRunMode.Client });
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);
            SafePublish(new NetworkPlayerTopologyChangedEvent { ConnectionsWithPlayer = numPlayers });
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);
            SafePublish(new NetworkPlayerTopologyChangedEvent { ConnectionsWithPlayer = numPlayers });
        }

        public override void OnServerSceneChanged(string sceneName)
        {
            base.OnServerSceneChanged(sceneName);
            SafePublish(new NetworkGameplaySceneChangedEvent { SceneNameOrPath = sceneName });
        }

        private void ApplyListenPort(ushort port)
        {
            if (transport is PortTransport pt)
                pt.Port = port;
        }

        private static void SafePublish<T>(T ev) where T : IGameEvent
        {
            var bus = GameEventBus.Instance;
            if (bus == null)
                return;
            bus.Initialize();
            bus.Publish(ev);
        }
    }
}
