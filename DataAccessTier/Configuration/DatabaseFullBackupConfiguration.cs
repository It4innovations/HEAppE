using HEAppE.DataAccessTier.Configuration.Shared;

namespace HEAppE.DataAccessTier.Configuration;

public sealed class DatabaseFullBackupConfiguration
{
    /// <summary>
    ///     Enable automatic scheduled full database backup.
    /// </summary>
    public static bool ScheduledBackupEnabled { get; set; }
    
    /// <summary>
    ///     Scheduled daily run time of backup in en-US time format.
    /// </summary>
    public static string ScheduledRuntime { get; set; } = "02:00:00";

    /// <summary>
    ///     Path to save database backup file.
    /// </summary>
    public static string LocalPath { get; set; } = @"C:\Temp";

    /// <summary>
    ///     NAS path to save database backup file.
    /// </summary>
    public static string NASPath { get; set; }

    /// <summary>
    ///     Backup file name prefix.
    /// </summary>
    public static string BackupFileNamePrefix { get; set; } = "Heappe";

    /// <summary>
    ///     Retention policy of backup files.
    /// </summary>
    public static RetentionPolicy RetentionPolicy { get; set; }
}
