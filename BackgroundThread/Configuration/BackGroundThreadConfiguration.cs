namespace HEAppE.BackgroundThread.Configuration;

/// <summary>
///     Background Thread configuration
/// </summary>
public sealed class BackGroundThreadConfiguration
{
    public static BackGroundThreadConfiguration Current { get; set; } = new();

    /// <summary>
    ///     Get all jobs information check in seconds
    /// </summary>
    public int GetAllJobsInformationCheck { get; set; } = 30;

    /// <summary>
    ///     Close connection to finished jobs check in seconds
    /// </summary>
    public int CloseConnectionToFinishedJobsCheck { get; set; } = 30;

    /// <summary>
    ///     Cluster account rotation job check in seconds
    /// </summary>
    public int ClusterAccountRotationJobCheck { get; set; } = 30;

    /// <summary>
    ///     Remove unused temporary file transfer key in seconds
    /// </summary>
    public int FileTransferKeyRemovalCheck { get; set; } = 10800;

    public int RoleAssignmentSyncCheck { get; set; } = 3600; //hour

    public sealed class ClusterProjectCredentialsCheckConfiguration
    {
        public bool IsEnabled { get; set; } = false;

        public int IntervalMinutes { get; set; } = 60;
    };

    public ClusterProjectCredentialsCheckConfiguration ClusterProjectCredentialsCheckSettings { get; set; } = new();

    public sealed class ExternalServiceHealthMonitoringConfiguration
    {
        /// <summary>
        /// Whether the periodic external service health check is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// How often to run the health check, in seconds.
        /// </summary>
        public int IntervalSeconds { get; set; } = 60;

        /// <summary>
        /// How many days of telemetry logs to retain.
        /// </summary>
        public int RetentionDays { get; set; } = 30;
    };

    public ExternalServiceHealthMonitoringConfiguration ExternalServiceHealthMonitoringSettings { get; set; } = new();
}