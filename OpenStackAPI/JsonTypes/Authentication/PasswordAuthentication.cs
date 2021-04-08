using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class PasswordAuthentication
    {
        [JsonProperty("user")]
        public User User { get; set; }
    }
}