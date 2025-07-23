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
    public static int TimeoutSeconds { get; set; } = 30;
}