namespace SshCaAPI.Configuration
{
    public class SshCaSettings
    {
        /// <summary>
        ///     Client base URI
        /// </summary>
        public static string BaseUri { get; set; } = "localhost";
        
        /// <summary>
        ///    Use certificate authority for authentication
        /// </summary>
        
        public static bool UseCertificateAuthorityForAuthentication { get; set; } = false;

        /// <summary>
        ///     Certification authority name
        /// </summary>
        public static string CAName { get; set; } = "sshca";

        /// <summary>
        ///     Client authentication token (only used for testing purpose)
        /// </summary>
        public static string? Token { get; set; }

        /// <summary>
        ///     Client connection timeout in seconds
        /// </summary>
        public static double ConnectionTimeoutInSeconds { get; set; } = 15;
    }
}
