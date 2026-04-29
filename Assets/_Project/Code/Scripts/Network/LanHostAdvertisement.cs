namespace Gameplay.Network
{
    /// <summary>
    /// 局域网发现结果摘要（与 <see cref="ILanLobbyBrowser"/> 配合使用）。
    /// </summary>
    public sealed class LanHostAdvertisement
    {
        public string EndPointHint;
        public ushort Port;
        /// <summary> UDP 载荷或自定义摘要（由 Mirror NetworkDiscovery 等映射）。 </summary>
        public string SessionKeyOrEmpty;
    }
}
