namespace HEAppE.BusinessLogicTier.Configuration
{
    /// <summary>
    /// Business logic configuration
    /// </summary>
    public sealed class BusinessLogicConfiguration
    {
        #region Properties
        /// <summary>
        /// Account rotation
        /// </summary>
        public static bool ClusterAccountRotation { get; set; }

        /// <summary>
        /// HTTP requeues connection timeout
        /// </summary>
        public static int HTTPRequestConnectionTimeout { get; set; } = 10000;
        #endregion
    }
}
