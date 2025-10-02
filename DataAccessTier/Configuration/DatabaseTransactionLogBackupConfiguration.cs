using HEAppE.DataAccessTier.Configuration.Shared;

namespace HEAppE.DataAccessTier.Configuration;

public sealed class DatabaseTransactionLogBackupConfiguration
{
    /// <summary>
    ///     Enable automatic scheduled database transaction logs backup.
    /// </summary>
    public static bool ScheduledBackupEnabled { get; set; }

    /// <summary>
    ///     Run backup every specified minutes.
    /// </summary>
    public static int BackupScheduleIntervalInMinutes { get; set; } = 60;

    /// <summary>
    ///     Path to save database transaction log backup file.
    /// </summary>
    public static string LocalPath { get; set; } = @"C:\Temp";

    /// <summary>
    ///     NAS path to save database transaction log backup file.
    /// </summary>
    public static string NASPath { get; set; }

    /// <summary>
    ///     Backup file name prefix.
    /// </summary>
    public static string BackupFileNamePrefix { get; set; } = "Heappe";

    /// <summary>
    ///     Retention policy of transaction log backup files.
    /// </summary>
    public static RetentionPolicy RetentionPolicy { get; set; }
}
