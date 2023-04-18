using Newtonsoft.Json;

namespace HEAppE.ExternalAuthentication.DTO.JsonTypes
{
    /// <summary>
    /// OpenId user authentication result
    /// <note>https://connect2id.com/products/server/docs/api/token#token-response</note>
    /// </summary>
    public class OpenIdUserAuthenticationResult
    {
        /// <summary>
        /// The access token issued by the server.
        /// </summary>
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// The lifetime of the access token, in seconds.
        /// </summary>
        [JsonProperty("expires_in")]
        public long ExpiresIn { get; set; }

        /// <summary>
        /// The lifetime of the refresh token, in seconds.
        /// </summary>
        [JsonProperty("refresh_expires_in")]
        public long RefreshExpiresIn { get; set; }

        /// <summary>
        /// Optional refresh token, which can be used to obtain new access tokens. Issued only for long-lived authorisations that permit it.
        /// </summary>
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        /// <summary>
        /// Set to bearer.
        /// </summary>
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        /// <summary>
        /// Not before policy.
        /// </summary>
        [JsonProperty("not-before-policy")]
        public long NotBeforePolicy { get; set; }

        /// <summary>
        /// Optional identity token, issued for the code and password grants. Not provided for client credentials grants.
        /// </summary>
        [JsonProperty("id_token")]
        public string IdToken { get; set; }

        /// <summary>
        /// Represents the End-User's login state at the OP. It MUST NOT contain the space (" ") character. This value is opaque to the RP. This is REQUIRED if session management is supported. 
        /// </summary>
        [JsonProperty("session_state")]
        public string SessionState { get; set; }

        /// <summary>
        /// The scope of the access token.
        /// </summary>
        [JsonProperty("scope")]
        public string Scope { get; set; }
    }
}
