namespace HEAppE.ExternalAuthentication.Configuration;

public class LexisTokenFlowConfiguration
{
    public bool IsEnabled { get; set; } = false;
    public bool UseExpirioServiceForTokenExchange { get; set; } = true;

    /// <summary>
    /// Whether to introspect the incoming LEXIS token against the LEXIS Keycloak before exchanging it (e.g., via Expirio for a FIP token).
    /// </summary>
    public bool IntrospectLexisToken { get; set; } = false;

    /// <summary>
    /// Optional explicit OIDC Authority URL for LEXIS Keycloak introspection.
    /// If empty, constructed from BaseUrl and Realm ({BaseUrl}/realms/{Realm}).
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Optional explicit introspection endpoint URL for LEXIS Keycloak.
    /// If empty, discovered from Authority/.well-known/openid-configuration or constructed as {Authority}/protocol/openid-connect/token/introspect.
    /// </summary>
    public string IntrospectionEndpoint { get; set; } = string.Empty;

    // Keycloak / LEXIS AAI DEV settings
    public string BaseUrl { get; set; } = string.Empty;      
    public string Realm { get; set; } = string.Empty;
    public string Broker { get; set; } = string.Empty;
    public string Scope { get; set; } = "openid profile email";

    // IdP / HEAppE target
    public string ClientId { get; set; } = "heappe";
    public string ClientSecret { get; set; } = string.Empty;
    public double ConnectionTimeoutInSeconds { get; set; } = 15;

    /// <summary>
    /// Resolves the effective OIDC authority URL from Authority or BaseUrl + Realm.
    /// </summary>
    public string GetEffectiveAuthority()
    {
        if (!string.IsNullOrWhiteSpace(Authority))
        {
            return Authority.TrimEnd('/');
        }

        if (!string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(Realm))
        {
            return $"{BaseUrl.TrimEnd('/')}/realms/{Realm}";
        }

        if (!string.IsNullOrWhiteSpace(BaseUrl))
        {
            return BaseUrl.TrimEnd('/');
        }

        return string.Empty;
    }
}