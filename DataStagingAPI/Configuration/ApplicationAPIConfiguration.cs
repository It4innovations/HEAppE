namespace HEAppE.DataStagingAPI.Configuration
{
#nullable disable
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
        /// Swagger documentation settings
        /// </summary>
        public static SwaggerConfiguration SwaggerDocSettings { get; } = new SwaggerConfiguration();
        #endregion
    }
}
