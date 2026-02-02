using HEAppE.DomainObjects.Management;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.Service;

public interface IDatabaseBackupService
{
    Task<string> BackupDatabase();
    string BackupDatabaseTransactionLogs();
    List<DatabaseBackup> ListDatabaseBackups(DateTime? fromDateTime, DateTime? toDateTime, DatabaseBackupType type);
    void RestoreDatabase(string backupFileName, bool includeLogs);
}
