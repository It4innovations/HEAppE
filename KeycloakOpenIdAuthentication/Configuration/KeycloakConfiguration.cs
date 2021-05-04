using System.Collections.Generic;

namespace HEAppE.KeycloakOpenIdAuthentication.Configuration
{
    /// <summary>
    /// KeyCloak settings
    /// </summary>
    public class KeycloakConfiguration
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
        /// Home organization
        /// </summary>
        public static string Organization { get; set; }

        /// <summary>
        /// User prefix in HEAppE DB
        /// </summary>
        public static string HEAppEUserPrefix { get; set; }

        /// <summary>
        /// Group name in HEAppE DB
        /// </summary>
        public static string HEAppEGroupName { get; set; }

        /// <summary>
        /// Allowed client Ids
        /// </summary>
        public static IEnumerable<string> AllowedClientIds { get; set; } = new List<string>() { "LEXIS_ORCHESTRATOR_YORC", "LEXIS_DDI_STAGING_API", "LEXIS_ORCHESTRATOR_BUSINESS_LOGIC", "admin-cli" };
        #endregion
    }
}