using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication
{
    public class AuthenticationResponse
    {
        #region Properties
        [JsonProperty("token")]
        public Token Token { get; set; }

        [JsonIgnore]
        public string AuthToken { get; set; }
        #endregion
    }
}