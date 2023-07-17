namespace HEAppE.DataStagingAPI.Configuration
{
#nullable disable
    /// <summary>
    /// Swagger setting from config
    /// </summary>
    public sealed class SwaggerOptions
    {
        #region Instances
        /// <summary>
        /// Host address with schema
        /// </summary>
        private string _host;

        /// <summary>
        /// Host postfix address
        /// </summary>
        private string _hostPostfix;

        /// <summary>
        /// Swagger prefix
        /// </summary>
        private string _prefixDocPath;
        #endregion
        #region Properties
        /// <summary>
        /// API Version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// API Title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// API Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Host address with schema
        /// </summary>
        public string Host
        {
            get => _host;
            set => _host = Extensions.RemoveCharacterFromBeginAndEnd(value, '/');
        }
        /// <summary>
        /// Host postfix address
        /// </summary>
        public string HostPostfix
        {
            get => _hostPostfix;
            set => _hostPostfix = Extensions.RemoveCharacterFromBeginAndEnd(value, '/');
        }
        /// <summary>
        /// Swagger prefix
        /// </summary>
        public string PrefixDocPath
        {
            get => _prefixDocPath;
            set => _prefixDocPath = Extensions.RemoveCharacterFromBeginAndEnd(value, '/');
        }

        /// <summary>
        /// API Term of usage
        /// </summary>
        public string TermOfUsageUrl { get; set; }

        /// <summary>
        /// API Contact name
        /// </summary>
        public string ContactName { get; set; }

        /// <summary>
        /// API Contact email
        /// </summary>
        public string ContactEmail { get; set; }

        /// <summary>
        /// API Contact Url
        /// </summary>
        public string ContactUrl { get; set; }

        /// <summary>
        /// API License
        /// </summary>
        public string License { get; set; }

        /// <summary>
        /// API License Url
        /// </summary>
        public string LicenseUrl { get; set; }
        #endregion
    }
}
