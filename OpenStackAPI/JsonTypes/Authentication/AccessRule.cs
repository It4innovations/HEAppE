using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class AccessRule
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("service")]
        public string Service { get; set; }
    }
}