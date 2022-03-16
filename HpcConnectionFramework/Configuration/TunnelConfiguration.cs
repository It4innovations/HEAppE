namespace HEAppE.HpcConnectionFramework.Configuration
{
    /// <summary>
    /// Tunnel configuration
    /// </summary>
    public class TunnelConfiguration
    {
        #region Properties
        /// <summary>
        /// Localhost name
        /// </summary>
        public static string LocalhostName { get; set; } = "127.0.0.1";

        /// <summary>
        /// Minimal local port
        /// </summary>
        public static int MinLocalPort { get; set; }

        /// <summary>
        /// Maximal local port
        /// </summary>
        public static int MaxLocalPort { get; set; }
        #endregion
    }
}
