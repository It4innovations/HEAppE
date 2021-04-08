using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class Project : Identifiable
    {
        [JsonProperty("domain")]
        public Domain Domain { get; set; }
    }
}