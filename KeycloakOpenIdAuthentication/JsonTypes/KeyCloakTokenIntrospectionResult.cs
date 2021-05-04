using Newtonsoft.Json;
using System.Collections.Generic;

namespace HEAppE.KeycloakOpenIdAuthentication.JsonTypes
{
    /// <summary>
    /// KeyCloak Extension for token introspection result
    /// </summary>
    public sealed class KeycloakTokenIntrospectionResult : TokenIntrospectionResult
    {
        /// <summary>
        /// Type of the token, set to Bearer.
        /// </summary>
        [JsonProperty("typ")]
        private string Typ { get { return TokenType; }set { TokenType = value; }  }

        /// <summary>
        /// Authorized Party - the party to which the ID Token was issued.
        /// </summary>
        [JsonProperty("azp")]
        public string Azp { get; set; }

        /// <summary>
        /// Time when the End-User authentication occurred.
        /// </summary>
        [JsonProperty("auth_time")]
        public int AuthTime { get; set; }

        /// <summary>
        /// Session State attribute.
        /// </summary>
        [JsonProperty("session_state")]
        public string SessionState { get; set; }

        /// <summary>
        /// The full name of the end-user.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The given or first name of the end-user.
        /// </summary>
        [JsonProperty("given_name")]
        public string GivenName { get; set; }

        /// <summary>
        /// The surname(s) or last name(s) of the end-user.
        /// </summary>
        [JsonProperty("family_name")]
        public string FamilyName { get; set; }

        /// <summary>
        /// The username by which the end-user wants to be referred to at the client application.
        /// </summary>
        [JsonProperty("preferred_username")]
        public string PreferredUsername { get; set; }

        /// <summary>
        /// The end-user's preferred email address.
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }

        /// <summary>
        /// True if the end-user's email address has been verified, else false.
        /// </summary>
        [JsonProperty("email_verified")]
        public bool EmailVerified { get; set; }

        /// <summary>
        /// Authentication Context Class Reference.
        /// </summary>
        [JsonProperty("acr")]
        public string Acr { get; set; }

        /// <summary>
        /// Allowed origins.
        /// </summary>
        [JsonProperty("allowed-origins")]
        public IEnumerable<string> AllowedOrigins { get; set; }
    }
}
