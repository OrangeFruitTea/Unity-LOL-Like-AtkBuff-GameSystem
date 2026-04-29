using System;
using System.Collections.Generic;

namespace Gameplay.Network
{
    /// <summary> 占位：未启用 NetworkDiscovery 时 UI 仍可引用非空实例。不会触发 HostListChanged。 </summary>
    public sealed class NullLanLobbyBrowser : ILanLobbyBrowser
    {
        private static readonly LanHostAdvertisement[] Empty = System.Array.Empty<LanHostAdvertisement>();

        public IReadOnlyList<LanHostAdvertisement> VisibleHosts => Empty;

#pragma warning disable CS0067
        /// <inheritdoc />
        public event Action HostListChanged;
#pragma warning restore CS0067

        public void BeginListenForHosts() { }

        public void StopListenForHosts() { }

        public void RequestConnect(in LanHostAdvertisement host) { }
    }
}
