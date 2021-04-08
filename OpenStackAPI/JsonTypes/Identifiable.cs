using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes
{
    public abstract class Identifiable
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}