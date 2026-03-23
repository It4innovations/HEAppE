using HEAppE.DataAccessTier.Configuration.Shared;

namespace HEAppE.DataAccessTier.Configuration;

public sealed class DatabaseFullBackupConfiguration
{
    public static DatabaseFullBackupConfiguration Current { get; set; } = new();

    /// <summary>
    ///     Enable automatic scheduled full database backup.
    /// </summary>
    public bool ScheduledBackupEnabled { get; set; }
    
    /// <summary>
    ///     Scheduled daily run time of backup in en-US time format.
    /// </summary>
    public string ScheduledRuntime { get; set; } = "02:00:00";

    /// <summary>
    ///     Path to save database backup file.
    /// </summary>
    public string LocalPath { get; set; } = @"/opt/heappe/backups/database";

    /// <summary>
    ///     NAS path to save database backup file.
    /// </summary>
    public string NASPath { get; set; }

    /// <summary>
    ///     Backup file name prefix.
    /// </summary>
    public string BackupFileNamePrefix { get; set; } = "HEAppE";

    /// <summary>
    ///     Retention policy of backup files.
    /// </summary>
    public RetentionPolicy RetentionPolicy { get; set; }
}
