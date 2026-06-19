namespace HEAppE.DataStagingAPI.Configuration;
#nullable disable
public class ApplicationAPIOptions
{
    public string[] AllowedHosts { get; set; } = System.Array.Empty<string>();

    public string AuthenticationParamHeaderName { get; set; } = "AuthKey";

    public string AuthenticationToken { get; set; }

    public DeploymentOptions DeploymentConfiguration { get; set; } = new();

    public SwaggerOptions SwaggerConfiguration { get; set; } = new();
}