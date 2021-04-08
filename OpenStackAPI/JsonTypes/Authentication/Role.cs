using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class Role
    {
        [JsonProperty("role")]
        public string Name { get; set; }
    }
}