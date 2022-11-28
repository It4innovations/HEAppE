using System;
using System.Collections.Generic;

namespace HEAppE.ExternalAuthentication.Configuration
{
    /// <summary>
    /// KeyCloak settings
    /// </summary>
    public class ExternalAuthConfiguration
    {
        #region Properties
        /// <summary>
        /// Base URL
        /// </summary>
        public static string BaseUrl { get; set; }

        /// <summary>
        /// Client protocol
        /// </summary>
        public static string Protocol { get; set; }

        /// <summary>
        /// Realm name
        /// </summary>
        public static string RealmName { get; set; }

        /// <summary>
        /// Client id/name
        /// </summary>
        public static string ClientId { get; set; }

        /// <summary>
        /// Client Secret
        /// </summary>
        public static string SecretId { get; set; }

        /// <summary>
        /// Client connection timeout in miliseconds
        /// </summary>
        public static int ConnectionTimeout { get; set; } = 15000;

        /// <summary>
        /// Allowed client Ids
        /// </summary>
        public static IEnumerable<string> AllowedClientIds { get; set; }

        /// <summary>
        /// Mapping role from OpenId to HEAppE internal roles
        /// </summary>
        public static Dictionary<string,string> RoleMapping { get; set; }

        /// <summary>
        /// Projects
        /// </summary>
        public static IEnumerable<ExternalAuthProjectConfiguration> Projects { get; set; }
        
        /// <summary>
        /// User prefix in HEAppE DB
        /// </summary>
        public static string HEAppEUserPrefix { get; set; }
        #endregion
    }
}