using HEAppE.DataAccessTier.Configuration.Shared;

namespace HEAppE.DataAccessTier.Configuration;

public sealed class DatabaseTransactionLogBackupConfiguration
{
    public static DatabaseTransactionLogBackupConfiguration Current { get; set; } = new();

    /// <summary>
    ///     Enable automatic scheduled database transaction logs backup.
    /// </summary>
    public bool ScheduledBackupEnabled { get; set; }

    /// <summary>
    ///     Run backup every specified minutes.
    /// </summary>
    public int BackupScheduleIntervalInMinutes { get; set; } = 60;

    /// <summary>
    ///     Path to save database transaction log backup file.
    /// </summary>
    public string LocalPath { get; set; } = @"/opt/heappe/backups/database";

    /// <summary>
    ///     NAS path to save database transaction log backup file.
    /// </summary>
    public string NASPath { get; set; }

    /// <summary>
    ///     Backup file name prefix.
    /// </summary>
    public string BackupFileNamePrefix { get; set; } = "Heappe";

    /// <summary>
    ///     Retention policy of transaction log backup files.
    /// </summary>
    public RetentionPolicy RetentionPolicy { get; set; }
}
