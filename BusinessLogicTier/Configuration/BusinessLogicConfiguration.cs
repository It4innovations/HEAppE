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
        /// Limit of generated file transfer key per job
        /// </summary>
        public static int GeneratedFileTransferKeyLimitPerJob { get; set; } = 5;

        /// <summary>
        /// Validity of temporary transfer keys in hours
        /// </summary>
        public static int ValidityOfTemporaryTransferKeysInHours { get; set; } = 24;

        /// <summary>
        /// HTTP requeues connection timeout
        /// </summary>
        public static int HTTPRequestConnectionTimeout { get; set; } = 10000;
        #endregion
    }
}
