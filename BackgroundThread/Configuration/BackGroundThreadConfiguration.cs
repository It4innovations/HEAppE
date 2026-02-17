namespace HEAppE.BackgroundThread.Configuration;

/// <summary>
///     Background Thread configuration
/// </summary>
public sealed class BackGroundThreadConfiguration
{
    /// <summary>
    ///     Get all jobs information check in seconds
    /// </summary>
    public static int GetAllJobsInformationCheck { get; set; } = 30;

    /// <summary>
    ///     Close connection to finished jobs check in seconds
    /// </summary>
    public static int CloseConnectionToFinishedJobsCheck { get; set; } = 30;

    /// <summary>
    ///     Cluster account rotation job check in seconds
    /// </summary>
    public static int ClusterAccountRotationJobCheck { get; set; } = 30;

    /// <summary>
    ///     Remove unused temporary file transfer key in seconds
    /// </summary>
    public static int FileTransferKeyRemovalCheck { get; set; } = 10800;

    public static int RoleAssignmentSyncCheck { get; set; } = 3600; //hour

    public sealed class ClusterProjectCredentialsCheckConfiguration
    {
        public static bool IsEnabled { get; set; } = false;

        public static int IntervalMinutes { get; set; } = 60;
    };

    public static ClusterProjectCredentialsCheckConfiguration ClusterProjectCredentialsCheckSettings { get; } = new();
}