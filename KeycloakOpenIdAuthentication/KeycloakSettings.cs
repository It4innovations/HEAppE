namespace HEAppE.KeycloakOpenIdAuthentication
{
    /// <summary>
    /// KeyCloak settings
    /// </summary>
    public class KeycloakSettings
    {
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
    }
}