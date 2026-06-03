namespace HEAppE.DataAccessTier.Configuration;

public class DatabaseMigrationSettings
{
    public static bool AutoMigrateDatabase { get; set; }
    public static bool DisableSeedingAndMigration { get; set; }
}