using System;
using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication
{
    public class User : Identifiable
    {
        #region Properties
        [JsonProperty("domain")]
        public Domain Domain { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("password_expires_at")]
        public DateTime? PasswordExpiresAt { get; set; }
        #endregion
    }
}