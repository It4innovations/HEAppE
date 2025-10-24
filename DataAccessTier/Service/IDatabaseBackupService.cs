using HEAppE.DomainObjects.Management;
using System;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.Service;

public interface IDatabaseBackupService
{
    string BackupDatabase();
    string BackupDatabaseTransactionLogs();
    List<DatabaseBackup> ListDatabaseBackups(DateTime? fromDateTime, DateTime? toDateTime, DatabaseBackupType type);
    void RestoreDatabase(string backupFileName, bool includeLogs);
}
