using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using HEAppE.RestUtils.JsonConvertors;

namespace HEAppE.KeycloakOpenIdAuthentication.JsonTypes
{
    public class RoleList
    {
        [JsonProperty("roles")]
        public List<string> Roles { get; set; }
    }

    public class TokenIntrospectionResult
    {
        /// <summary>
        /// A unique identifier for the signed authentication request.
        /// </summary>
        [JsonProperty("jti")]
        public string Jti { get; set; }

        /// <summary>
        /// An expiration time that limits the validity lifetime of the signed authentication request.
        /// </summary>
        [JsonProperty("exp")]
        public int Exp { get; set; }

        /// <summary>
        /// The time before which the signed authentication request is unacceptable.
        /// </summary>
        [JsonProperty("nbf")]
        public int Nbf { get; set; }

        /// <summary>
        /// The time at which the signed authentication request was created.
        /// </summary>
        [JsonProperty("iat")]
        public int Iat { get; set; }

        /// <summary>
        /// The Issuer claim MUST be the client_id of the OAuth Client.
        /// </summary>
        [JsonProperty("iss")]
        public string Iss { get; set; }

        /// <summary>
        /// Subject identifier. A locally unique and never reassigned identifier within the Issuer for the End-User, which is intended to be consumed by the Client.
        /// In case of Keycloak it is Unique User Id (UUID).
        /// </summary>
        [JsonProperty("sub")]
        public string Sub { get; set; }

        [JsonProperty("typ")]
        public string Typ { get; set; }

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

        [JsonProperty("session_state")]
        public string SessionState { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("given_name")]
        public string GivenName { get; set; }

        [JsonProperty("family_name")]
        public string FamilyName { get; set; }

        [JsonProperty("preferred_username")]
        public string PreferredUsername { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("email_verified")]
        public bool EmailVerified { get; set; }

        /// <summary>
        /// Authentication Context Class Reference.
        /// </summary>
        [JsonProperty("acr")]
        public string Acr { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("active", Required = Required.Always)]
        public bool Active { get; set; }


        [JsonProperty("aud")]
        [JsonConverter(typeof(SingleValueOrArrayConvertor<string>))]
        public List<string> Aud { get; set; } // NOTE(Moravec): This value can be both array or single string according to: https://openid.net/specs/openid-connect-core-1_0.html

        [JsonProperty("allowed-origins")]
        public List<string> AllowedOrigins { get; set; }

        /// <summary>
        /// Not interesting for us at this moment.
        /// </summary>
        [JsonProperty("realm_access")]
        public RoleList EffectiveRoles { get; set; }

        /// <summary>
        /// Dictionary of client role mappings. For each key, which represents the client name, there is list of roles assigned to this user.
        /// </summary>
        [JsonProperty("resource_access")]
        public Dictionary<string, RoleList> ClientRoles { get; set; }
    }
}