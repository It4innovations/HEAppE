using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class ApplicationCredentialsResponse
    {
        [JsonProperty("application_credential")]
        public ApplicationCredentials ApplicationCredentials { get; set; }
    }
}