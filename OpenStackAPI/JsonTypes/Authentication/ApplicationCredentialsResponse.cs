using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class ApplicationCredentialsResponse
    {
        #region Properties
        [JsonProperty("application_credential")]
        public ApplicationCredentials ApplicationCredentials { get; set; }
        #endregion
    }
}