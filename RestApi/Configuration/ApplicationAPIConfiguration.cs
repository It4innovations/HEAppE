using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        /// Instance name
        /// </summary>
        public static string InstanceName { get; set; }

        /// <summary>
        /// Instance version
        /// </summary>
        public static string Version { get; set; }

        /// <summary>
        /// Instance IP
        /// </summary>
        public static string DeployedIPAddress { get; set; }

        /// <summary>
        /// Swagger documentation settings
        /// </summary>
        public static SwaggerConfiguration SwaggerDocSettings { get; } = new SwaggerConfiguration();
        #endregion
    }
}
