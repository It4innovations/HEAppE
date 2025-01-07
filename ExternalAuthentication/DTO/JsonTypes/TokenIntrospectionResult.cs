using System.Collections.Generic;
using HEAppE.RestUtils.JsonConvertors;
using Newtonsoft.Json;

namespace HEAppE.ExternalAuthentication.DTO.JsonTypes;

/// <summary>
///     Token introspection result
///     <note>https://connect2id.com/products/server/docs/api/token-introspection</note>
/// </summary>
public class TokenIntrospectionResult
{
    /// <summary>
    ///     If true the token is valid, active and, if it has an explicit audience, the calling resource server is in it. If
    ///     false the token is invalid, has been revoked, has expired or the caller (resource server) is not in its audience,
    ///     in which case no further details are provided.
    /// </summary>
    [JsonProperty("active", Required = Required.Always)]
    public bool Active { get; set; }

    /// <summary>
    ///     The scope values for the token.
    /// </summary>
    [JsonProperty("scope")]
    public string Scope { get; set; }

    /// <summary>
    ///     The identifier of the OAuth 2.0 client to which the token was issued.
    /// </summary>
    [JsonProperty("client_id")]
    public string ClientId { get; set; }

    /// <summary>
    ///     Username of the resource owner who authorised the token.
    /// </summary>
    [JsonProperty("username")]
    public string Username { get; set; }

    /// <summary>
    ///     Type of the token, set to Bearer.
    /// </summary>
    [JsonProperty("token_type")]
    public string TokenType { get; set; }

    /// <summary>
    ///     The token expiration time, as number of seconds since the Unix epoch (1970-01-01T0:0:0Z) as measured in UTC until
    ///     the date/time. Has the same semantics as the standard JWT claim name.
    /// </summary>
    [JsonProperty("exp")]
    public long Exp { get; set; }

    /// <summary>
    ///     The token issue time, as number of seconds since the Unix epoch (1970-01-01T0:0:0Z) as measured in UTC until the
    ///     date/time. Has the same semantics as the standard JWT claim name.
    /// </summary>
    [JsonProperty("iat")]
    public long Iat { get; set; }

    /// <summary>
    ///     The token use-not-before time, as number of seconds since the Unix epoch (1970-01-01T0:0:0Z) as measured in UTC
    ///     until the date/time. Has the same semantics as the standard JWT claim name.
    /// </summary>
    [JsonProperty("nbf")]
    public long Nbf { get; set; }

    /// <summary>
    ///     The subject of the token. Typically the user identifier of the resource owner who authorised the token. Has the
    ///     same semantics as the standard JWT claim name.
    /// </summary>
    [JsonProperty("sub")]
    public string Sub { get; set; }

    /// <summary>
    ///     Audience values for the token. Has the same semantics as the standard JWT claim name. If the token has an explicit
    ///     audience only the client_id of the calling resource server will be included; any other identifiers will be omitted.
    /// </summary>
    [JsonProperty("aud")]
    [JsonConverter(typeof(SingleValueOrArrayConvertor<string>))]
    public List<string> Aud { get; set; }

    /// <summary>
    ///     The token issuer (the OpenID Provider / Authorisation Server issuer URI). Has the same semantics as the standard
    ///     JWT claim name.
    /// </summary>
    [JsonProperty("iss")]
    public string Iss { get; set; }

    /// <summary>
    ///     Identifier for the token. Has the same semantics as the standard JWT claim name.
    /// </summary>
    [JsonProperty("jti")]
    public string Jti { get; set; }
}