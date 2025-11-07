namespace HEAppE.DataAccessTier.Configuration.Shared;

/// <summary>
///     Retention category of database backups.
/// </summary>
public enum BackupRetentionCategory : byte
{
    Daily,
    Weekly,
    Monthly,
}

/// <summary>
///     Class representing configuration of database backups retention policy.
/// </summary>
public class RetentionPolicy
{
    public int Daily { get; set; } = 7;
    public int Weekly { get; set; } = 4;
    public int Monthly { get; set; } = 12;
}