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
        /// Swagger documentation settings
        /// </summary>
        public static SwaggerConfiguration SwaggerDocSettings { get; } = new SwaggerConfiguration();

        /// <summary>
        /// Minute interval for checking for expired tokens at memory cache
        /// </summary>
        public static long MemoryCacheMinuteInterval { get; set; } = 1;
        #endregion
    }
}
