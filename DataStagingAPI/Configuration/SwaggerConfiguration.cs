﻿namespace HEAppE.DataStagingAPI.Configuration
{
#nullable disable
    /// <summary>
    /// Swagger setting from config
    /// </summary>
    public sealed class SwaggerConfiguration
    {
        #region Instances
        /// <summary>
        /// Host address with schema
        /// </summary>
        private static string _host;

        /// <summary>
        /// Host postfix address
        /// </summary>
        private static string _hostPostfix;

        /// <summary>
        /// Swagger prefix
        /// </summary>
        private static string _prefixDocPath;
        #endregion
        #region Properties
        /// <summary>
        /// API Version
        /// </summary>
        public static string Version { get; set; }

        /// <summary>
        /// API Title
        /// </summary>
        public static string Title { get; set; }

        /// <summary>
        /// API Description
        /// </summary>
        public static string Description { get; set; }

        /// <summary>
        /// Host address with schema
        /// </summary>
        public static string Host
        {
            get => _host;
            set => _host = Utils.RemoveCharacterFromBeginAndEnd(value, '/');
        }
        /// <summary>
        /// Host postfix address
        /// </summary>
        public static string HostPostfix
        {
            get => _hostPostfix;
            set => _hostPostfix = Utils.RemoveCharacterFromBeginAndEnd(value, '/');
        }
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
