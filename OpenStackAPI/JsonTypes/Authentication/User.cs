using System;
using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class User : Identifiable
    {
        [JsonProperty("domain")]
        public Domain Domain { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("password_expires_at")]
        public DateTime? PasswordExpiresAt { get; set; }
    }
}