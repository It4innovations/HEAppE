using Newtonsoft.Json;
using System.Collections.Generic;

namespace HEAppE.KeycloakOpenIdAuthentication.JsonTypes
{
    /// <summary>
    /// KeyCloak extension for user info result
    /// </summary>
    public sealed class KeyCloakUserInfoResult: UserInfoResult
    {
        /// <summary>
        /// UserAttributes
        /// </summary>
        [JsonProperty("attributes")]
        public IEnumerable<string> Attributes { get; set; }
    }
}
