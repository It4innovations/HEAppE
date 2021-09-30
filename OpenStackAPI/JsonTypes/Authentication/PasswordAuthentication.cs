using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class PasswordAuthentication
    {
        #region Properties
        [JsonProperty("user")]
        public User User { get; set; }
        #endregion
    }
}