namespace HEAppE.ExternalAuthentication.Configuration;

/// <summary>
/// Configuration for JWT token introspection
/// </summary>
public class JwtTokenIntrospectionConfiguration
{
    public static bool IsEnabled { get; set; } = false;
    public static string Authority { get; set; } = string.Empty;
    public static string ClientId { get; set; } = string.Empty;
    public static string ClientSecret { get; set; } = string.Empty;
    public static bool RequireHttps { get; set; } = false;
    public static bool ValidateIssuerName { get; set; } = false;
    public static bool ValidateEndpoints { get; set; } = false;

    public static TokenExchangeConfiguration TokenExchangeConfiguration { get; set; } = new();
}