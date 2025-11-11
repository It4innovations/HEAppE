namespace HEAppE.ExternalAuthentication.Configuration;

public class LexisTokenFlowConfiguration
{
    public bool IsEnabled { get; set; } = false;

    // Keycloak / LEXIS AAI DEV settings
    public string BaseUrl { get; set; } = string.Empty;      
    public string Realm { get; set; } = string.Empty;
    public string Scope { get; set; } = "openid profile email";

    // FIP / HEAppE target
    public string ClientId { get; set; } = "heappe";
    public string ClientSecret { get; set; } = string.Empty;
}