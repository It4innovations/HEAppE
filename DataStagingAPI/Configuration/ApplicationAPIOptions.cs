namespace HEAppE.DataStagingAPI.Configuration
{
#nullable disable
    public class ApplicationAPIOptions
    {
        public string[] AllowedHosts { get; set; }

        public string AuthenticationParamHeaderName { get; set; }

        public string AuthenticationToken { get; set; }

        public DeploymentOptions DeploymentConfiguration { get; set; }

        public SwaggerOptions SwaggerConfiguration { get; set; }
    }
}
