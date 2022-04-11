namespace HEAppE.BackgroundThread.Configuration
{
    /// <summary>
    /// Background Thread configuration
    /// </summary>
    public sealed class BackGroundThreadConfiguration
    {
        /// <summary>
        /// Get all jobs information check in seconds
        /// </summary>
        public static int GetAllJobsInformationCheck { get; set; } = 30;

        /// <summary>
        /// Close connection to finished jobs check in seconds
        /// </summary>
        public static int CloseConnectionToFinishedJobsCheck { get; set; } = 30;

        /// <summary>
        /// Cluster account rotation job check in seconds
        /// </summary>
        public static int ClusterAccountRotationJobCheck { get; set; } = 30;
    }
}
