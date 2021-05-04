using Newtonsoft.Json;
using System.Collections.Generic;

namespace HEAppE.KeycloakOpenIdAuthentication.JsonTypes
{
    /// <summary>
    /// KeyCloak extension for user info result
    /// </summary>
    public sealed class KeycloakUserInfoResult: UserInfoResult
    {
        /// <summary>
        /// UserAttributes
        /// </summary>
        [JsonProperty("attributes")]
        public AttributesResult Attributes { get; set; }
    }
}
