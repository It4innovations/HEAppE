using Newtonsoft.Json;

namespace HEAppE.KeycloakOpenIdAuthentication.JsonTypes
{
    public class AccessRightsResult
    {
        /// <summary>
        /// Organization name.
        /// </summary>
        [JsonProperty("ORG_UUID")]
        public string OrganizationName { get; set; }

        /// <summary>
        /// Project name.
        /// </summary>
        [JsonProperty("PRJ")]
        public string ProjectName { get; set; }
    }
}
