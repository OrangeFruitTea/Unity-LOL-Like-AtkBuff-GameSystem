using Basement.Json;

namespace Gameplay.Network
{
    /// <summary> 从 StreamingAssets 读取网络默认配置（与 Basement.Json 对齐）。 </summary>
    public interface INetworkConfigProvider
    {
        bool TryReload(out string error);
        JsonReadResult<NetworkConfigDto> Current { get; }
    }
}
