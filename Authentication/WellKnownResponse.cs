using System.Text.Json.Serialization;

namespace HEAppE.Authentication;

public class WellKnownResponse
{
    public string authorization_endpoint { get; set; }
    public string[] claim_types_supported { get; set; }
    public bool claims_parameter_supported { get; set; }
    public string[] claims_supported { get; set; }
    public string[] code_challenge_methods_supported { get; set; }
    public string device_authorization_endpoint { get; set; }
    public string[] grant_types_supported { get; set; }
    public string[] id_token_signing_alg_values_supported { get; set; }
    [JsonPropertyName("introspection_endpoint")]
    public string? IntrospectionEndpoint { get; set; }
    public string issuer { get; set; }
    public string jwks_uri { get; set; }
}