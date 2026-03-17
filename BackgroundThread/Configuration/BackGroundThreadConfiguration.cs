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
}