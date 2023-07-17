namespace HEAppE.RestApi.Configuration
{
    /// <summary>
    /// Application API configuration
    /// </summary>
    public sealed class ApplicationAPIConfiguration
    {
        #region Properties
        /// <summary>
        /// Allowed hosts
        /// </summary>
        public static string[] AllowedHosts { get; set; }

        /// <summary>
        /// Deployment settings
        /// </summary>
        public static DeploymentInformationsConfiguration DeploymentSettings { get; } = new DeploymentInformationsConfiguration();

        /// <summary>
        /// Swagger documentation settings
        /// </summary>
        public static SwaggerConfiguration SwaggerDocSettings { get; } = new SwaggerConfiguration();
        #endregion
    }
}
