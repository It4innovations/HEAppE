using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class System
    {
        [JsonProperty("all")]
        public bool All { get; set; }
    }
}