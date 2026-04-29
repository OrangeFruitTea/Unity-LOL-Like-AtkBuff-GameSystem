using System.IO;
using Basement.Json;
using UnityEngine;

namespace Gameplay.Network
{
    /// <summary> StreamingAssets/<see cref="DefaultRelativePath"/> を読み込み、Basement.Json 経由で検証済み結果を公開。 </summary>
    public sealed class NetworkConfigProvider : MonoBehaviour, INetworkConfigProvider
    {
        public const string DefaultRelativePath = "NetworkConfig.json";

        [SerializeField] private string _relativePath = DefaultRelativePath;

        private JsonReadResult<NetworkConfigDto> _cached = JsonReadResult<NetworkConfigDto>.Fail("<uninitialized>");

        public JsonReadResult<NetworkConfigDto> Current => _cached;

        private void Awake()
        {
            TryReload(out _);
            if (NetworkFacades.NetConfig == null)
                NetworkFacades.NetConfig = this;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(NetworkFacades.NetConfig, this))
                NetworkFacades.NetConfig = null;
        }

        public bool TryReload(out string error)
        {
            error = null;
            string full = Path.Combine(Application.streamingAssetsPath, _relativePath);
            if (!File.Exists(full))
            {
                error = $"file not found: {full}";
                _cached = JsonReadResult<NetworkConfigDto>.Fail(error);
                return false;
            }

            if (JsonManager.Instance == null)
            {
                error = "JsonManager is not initialized";
                _cached = JsonReadResult<NetworkConfigDto>.Fail(error);
                return false;
            }

            _cached = JsonManager.Instance.DeserializeFromFilePath<NetworkConfigDto>(full, JsonSerializerProfile.GameContent);
            if (!_cached.Success)
                error = _cached.Error ?? "Deserialize failed";

            return _cached.Success;
        }
    }
}
