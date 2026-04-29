using Newtonsoft.Json;

namespace Gameplay.Network
{
    public sealed class NetworkConfigDto
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [JsonProperty("defaultListenPort")]
        public ushort DefaultListenPort { get; set; } = 7777;

        [JsonProperty("fallbackBindAddressOrEmpty")]
        public string FallbackBindAddressOrEmpty { get; set; } = "";

        [JsonProperty("discoveryKeyOrEmpty")]
        public string DiscoveryKeyOrEmpty { get; set; } = "";
    }
}
