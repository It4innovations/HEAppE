namespace HEAppE.RestApi.Configuration
{
    /// <summary>
    /// Swagger setting from config
    /// </summary>
    public sealed class SwaggerConfiguration
    {
        #region Instances
        /// <summary>
        /// Swagger prefix
        /// </summary>
        private static string _prefixDocPath;
        #endregion
        #region Properties
        /// <summary>
        /// API Version
        /// </summary>
        public static string Version => DeploymentInformationsConfiguration.Version;

        /// <summary>
        /// API Title
        /// </summary>
        public static string Title { get; set; }

        /// <summary>
        /// Detailed Job Reporting API Title
        /// </summary>
        public static string DetailedJobReportingTitle { get; set; }

        /// <summary>
        /// API Description
        /// </summary>
        public static string Description { get; set; }

        /// <summary>
        /// Host adress with schema
        /// </summary>
        public static string Host => DeploymentInformationsConfiguration.Host;

        /// <summary>
        /// Host postfix addres
        /// </summary>
        public static string HostPostfix => DeploymentInformationsConfiguration.HostPostfix;

        /// <summary>
        /// Swagger prefix
        /// </summary>
        public static string PrefixDocPath
        {
            get => _prefixDocPath;
            set => _prefixDocPath = Utils.RemoveCharacterFromBeginAndEnd(value, '/');
        }

        /// <summary>
        /// API Term of usage
        /// </summary>
        public static string TermOfUsageUrl { get; set; }

        /// <summary>
        /// API Contact name
        /// </summary>
        public static string ContactName { get; set; }

        /// <summary>
        /// API Contact email
        /// </summary>
        public static string ContactEmail { get; set; }

        /// <summary>
        /// API Contact Url
        /// </summary>
        public static string ContactUrl { get; set; }

        /// <summary>
        /// API License
        /// </summary>
        public static string License { get; set; }

        /// <summary>
        /// API License Url
        /// </summary>
        public static string LicenseUrl { get; set; }
        #endregion
    }
}
