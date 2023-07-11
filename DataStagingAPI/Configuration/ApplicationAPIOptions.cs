namespace HEAppE.DataStagingAPI.Configuration
{
    public class ApplicationAPIOptions
    {
        public string[] AllowedHosts { get; set; }

        public string AuthenticationParamHeaderName { get; set; }

        public string AuthenticationToken { get; set; }

        public SwaggerOptions SwaggerConfiguration { get; set; }
    }
}
