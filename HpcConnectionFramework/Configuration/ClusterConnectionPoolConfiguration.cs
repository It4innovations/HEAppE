namespace HEAppE.HpcConnectionFramework.Configuration
{
    /// <summary>
    /// Cluster connection pool configuration
    /// </summary>
    public sealed class ClusterConnectionPoolConfiguration
    {
        #region Properties
        /// <summary>
        /// Connection pool min size
        /// </summary>
        public int ConnectionPoolMinSize { get; set; }

        /// <summary>
        /// Connection pool max size
        /// </summary>
        public int ConnectionPoolMaxSize { get; set; }

        /// <summary>
        /// Connection pool cleanning interval in seconds
        /// </summary>
        public int ConnectionPoolCleaningInterval { get; set; }

        /// <summary>
        /// Connection pool max unused interval in seconds
        /// </summary>
        public int ConnectionPoolMaxUnusedInterval { get; set; }
        #endregion
    }
}
