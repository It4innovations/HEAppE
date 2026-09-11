namespace HEAppE.ExternalAuthentication.Configuration;

/// <summary>
/// Configuration for a single named token exchange target.
/// Each target defines how to exchange a subject token for a token
/// usable by a specific downstream component (e.g., SSH CA, UserOrg, FirecREST).
/// 
/// Targets can be chained: SubjectTokenSource can reference another target's name,
/// creating a dependency graph that is resolved via topological sort.
/// </summary>
public class TokenExchangeTargetConfiguration
{
    /// <summary>
    /// Unique name identifying this exchange target (e.g., "sshca", "userorg", "firecrest").
    /// Used as a key for ITokenExchangeService.ExchangeAsync(name, token).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this exchange target is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The exchange strategy to use.
    /// Supported values:
    /// - "Rfc8693" – Standard Keycloak/OIDC token exchange (RFC 8693)
    /// - "Expirio" – Exchange via Expirio service
    /// - "ClientCredentials" – OAuth2 client_credentials grant
    /// - "KeycloakBroker" – RFC 8693 + Keycloak broker /token endpoint (two-step)
    /// </summary>
    public string Strategy { get; set; } = "Rfc8693";

    // === Connection Settings ===

    /// <summary>
    /// Token endpoint URL. If empty, discovered from Authority/.well-known/openid-configuration.
    /// </summary>
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// OIDC Authority URL (used for endpoint discovery if TokenEndpoint is empty).
    /// Falls back to JwtTokenIntrospectionConfiguration.Authority if empty.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Client ID for the exchange request authentication.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret for the exchange request authentication.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    // === RFC 8693 Settings ===

    /// <summary>
    /// Grant type. Defaults to RFC 8693 token exchange.
    /// </summary>
    public string GrantType { get; set; } = "urn:ietf:params:oauth:grant-type:token-exchange";

    /// <summary>
    /// Subject token type for RFC 8693.
    /// </summary>
    public string SubjectTokenType { get; set; } = "urn:ietf:params:oauth:token-type:access_token";

    /// <summary>
    /// Requested token type for RFC 8693.
    /// </summary>
    public string RequestedTokenType { get; set; } = "urn:ietf:params:oauth:token-type:access_token";

    /// <summary>
    /// Target audience for the exchanged token (e.g., "ssh-ca-service", "userorg-client").
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// OAuth scopes to request.
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    // === Expirio-Specific Settings ===

    /// <summary>
    /// For Expirio strategy: provider name.
    /// </summary>
    public string ExpirioProviderName { get; set; } = string.Empty;

    /// <summary>
    /// For Expirio strategy: client name.
    /// </summary>
    public string ExpirioClientName { get; set; } = string.Empty;

    // === KeycloakBroker-Specific Settings ===

    /// <summary>
    /// For KeycloakBroker strategy: broker alias (used in /broker/{alias}/token URL).
    /// </summary>
    public string BrokerAlias { get; set; } = string.Empty;

    // === Chaining & Auto-Exchange ===

    /// <summary>
    /// Which token to use as the subject_token for exchange.
    /// - "incoming" – the original Bearer token from the HTTP request
    /// - "&lt;target-name&gt;" – the output of another exchange target (enables chaining)
    /// </summary>
    public string SubjectTokenSource { get; set; } = "incoming";

    /// <summary>
    /// Key under which the exchanged token is stored in IRequestContext.ExchangedTokens.
    /// Defaults to Name if empty.
    /// </summary>
    public string StoreAs { get; set; } = string.Empty;

    /// <summary>
    /// The effective storage key (StoreAs if set, otherwise Name).
    /// </summary>
    public string EffectiveStoreKey => string.IsNullOrEmpty(StoreAs) ? Name : StoreAs;

    /// <summary>
    /// Whether this exchange should execute automatically during the auth middleware pipeline.
    /// If false, must be triggered explicitly via ITokenExchangeService.ExchangeAsync().
    /// </summary>
    public bool AutoExchange { get; set; } = false;

    /// <summary>
    /// Which phase of the middleware pipeline to auto-execute in.
    /// - "PreAuth" – before UseAuthentication() (e.g., LEXIS→IdP exchange)
    /// - "PostAuth" – after token validation (e.g., IdP→SshCA exchange)
    /// </summary>
    public string AutoExchangePhase { get; set; } = "PostAuth";

    /// <summary>
    /// Optional condition for auto-exchange. 
    /// A configuration key path (e.g., "SshCaSettings:UseCertificateAuthorityForAuthentication").
    /// If set, the target only auto-exchanges when this config value is true.
    /// Empty = always auto-exchange (when AutoExchange=true).
    /// </summary>
    public string AutoExchangeCondition { get; set; } = string.Empty;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    public double ConnectionTimeoutSeconds { get; set; } = 15;
}
