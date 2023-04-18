using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication
{
    public class AccessRule
    {
        #region Properties
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("service")]
        public string Service { get; set; }
        #endregion
    }
}