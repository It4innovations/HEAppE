using System.Collections.Generic;
using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class Identity
    {
        [JsonProperty("methods")]
        public List<string> Methods { get; set; }

        [JsonProperty("password")]
        public PasswordAuthentication Password { get; set; }
    }
}