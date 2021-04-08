using Newtonsoft.Json;

namespace HEAppE.KeycloakOpenIdAuthentication.JsonTypes
{
    public class UserRoleRepresentation
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("clientRole")]
        public bool ClientRole { get; set; }

        [JsonProperty("composite")]
        public bool Composite { get; set; }

        [JsonProperty("containerId")]
        public string ContainerId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        //attributes
        //composites
    }
}
