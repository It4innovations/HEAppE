using Newtonsoft.Json;

namespace HEAppE.ExternalAuthentication.JsonTypes
{
    public class GroupRepresentation
    {
        // https://www.keycloak.org/docs-api/11.0/rest-api/index.html#_grouprepresentation
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }
    }
}
