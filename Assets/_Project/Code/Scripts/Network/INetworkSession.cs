namespace Gameplay.Network
{
    /// <summary> 当前进程的网络会话只读快照。 </summary>
    public interface INetworkSessionReadOnly
    {
        bool IsHostRunning { get; }
        bool IsServerActive { get; }
        bool IsClientConnected { get; }
        bool IsOnlineSceneLoaded { get; }
        ushort ActivePort { get; }
        string LastRemoteAddressOrEmpty { get; }
    }

    /// <summary> 菜单/会话层控制 Host、Server、Client（底层为 Mirror）。 </summary>
    public interface INetworkSessionControl : INetworkSessionReadOnly
    {
        NetworkStartResult TryStartHost(ushort listenPort);
        NetworkStartResult TryStartServer(ushort listenPort);
        NetworkStartResult TryStartClient(string host, ushort port);

        void StopAll();

        /// <summary> 仅服务端；对齐 Mirror ServerChangeScene。 </summary>
        bool ServerChangeGameplayScene(string sceneAssetPathOrSceneName);
    }
}
