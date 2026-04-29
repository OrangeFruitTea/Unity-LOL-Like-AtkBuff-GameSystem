using System;
using System.Collections.Generic;

namespace Gameplay.Network
{
    /// <summary> 局域网房间列表占位；未接 NetworkDiscovery 时可用 <see cref="NullLanLobbyBrowser"/>。 </summary>
    public interface ILanLobbyBrowser
    {
        void BeginListenForHosts();
        void StopListenForHosts();

        IReadOnlyList<LanHostAdvertisement> VisibleHosts { get; }
        event Action HostListChanged;

        void RequestConnect(in LanHostAdvertisement host);
    }
}
