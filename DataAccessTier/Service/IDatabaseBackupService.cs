namespace HEAppE.DataAccessTier.Service;

public interface IDatabaseBackupService
{
    string BackupDatabase();
    string BackupDatabaseTransactionLogs();
}
