using HEAppE.DataAccessTier.Configuration;
using HEAppE.DomainObjects.Management;
using HEAppE.Exceptions.Internal;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HEAppE.DataAccessTier.Service;

internal class DatabaseBackupService : IDatabaseBackupService
{
    #region Constructors

    internal DatabaseBackupService(MiddlewareContext context)
    {
        _context = context;
    }

    #endregion

    #region Instances

    protected readonly MiddlewareContext _context;

    #endregion

    #region Methods

    /// <summary>
    ///     Full backup database.
    /// </summary>
    /// <returns>Path to created backup.</returns>
    public string BackupDatabase()
    {
        try
        {
            if (!DatabaseFullBackupCanBeDone())
                throw new DatabaseBackupException("FullBackupCantBeDone");

            var databaseName = _context.Database.GetDbConnection().Database;
            var backupFileName = $"{DatabaseFullBackupConfiguration.BackupFileNamePrefix}_FULL_{DateTime.Now:yyyyMMddHHmm}.bak";
            var backupPath = Path.Combine(DatabaseFullBackupConfiguration.LocalPath, backupFileName);

            string sql = $"BACKUP DATABASE [{databaseName}] TO DISK = N'{backupPath}' WITH INIT;";
            _context.Database.ExecuteSqlRaw(sql);

            // Copy to NAS
            if (!string.IsNullOrEmpty(DatabaseFullBackupConfiguration.NASPath))
            {
                var nasFile = Path.Combine(DatabaseFullBackupConfiguration.NASPath, backupFileName);
                File.Copy(backupPath, nasFile, overwrite: true);
            }

            return backupPath;
        }
        catch (Exception ex)
        {
            throw new DatabaseBackupException("FullBackupException", ex);
        }
    }

    /// <summary>
    ///     Database transaction logs backup.
    /// </summary>
    /// <returns>Path to created transaction logs backup.</returns>
    public string BackupDatabaseTransactionLogs()
    {
        try
        {
            if (!DatabaseLogsBackupCanBeDone())
                throw new DatabaseBackupException("BackupTransactionLogsCantBeDone");

            var databaseName = _context.Database.GetDbConnection().Database;
            var backupFileName = $"{DatabaseTransactionLogBackupConfiguration.BackupFileNamePrefix}_LOGS_{DateTime.Now:yyyyMMddHHmm}.trn";
            var backupPath = Path.Combine(DatabaseTransactionLogBackupConfiguration.LocalPath, backupFileName);

            string sql = $"BACKUP LOG [{databaseName}] TO DISK = N'{backupPath}' WITH INIT;";
            _context.Database.ExecuteSqlRaw(sql);

            // Copy to NAS
            if (!string.IsNullOrEmpty(DatabaseTransactionLogBackupConfiguration.NASPath))
            {
                var nasFile = Path.Combine(DatabaseTransactionLogBackupConfiguration.NASPath, backupFileName);
                File.Copy(backupPath, nasFile, overwrite: true);
            }

            return backupPath;
        }
        catch (Exception ex)
        {
            throw new DatabaseBackupException("TransactionLogsBackupException", ex);
        }
    }

    /// <summary>
    ///     List database backups.
    /// </summary>
    /// <param name="fromDateTime"></param>
    /// <param name="toDateTime"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public List<DatabaseBackup> ListDatabaseBackups(DateTime? fromDateTime, DateTime? toDateTime, DatabaseBackupType type)
    {
        try
        {
            return _context.Database.SqlQueryRaw<DatabaseBackup>(
            @"SELECT 
                  mf.physical_device_name AS Path,
                  CASE
                      WHEN mf.physical_device_name IS NULL THEN NULL
                      WHEN CHARINDEX('\', mf.physical_device_name) > 0
                          THEN RIGHT(mf.physical_device_name, CHARINDEX('\', REVERSE(mf.physical_device_name)) - 1)
                      WHEN CHARINDEX('/', mf.physical_device_name) > 0
                          THEN RIGHT(mf.physical_device_name, CHARINDEX('/', REVERSE(mf.physical_device_name)) - 1)
                      ELSE mf.physical_device_name
                  END AS FileName,
                  b.backup_finish_date AS TimeStamp,
                  CAST(b.backup_size / 1024 / 1024 AS DECIMAL(10,2)) AS FileSizeMB,
                  CASE b.type WHEN 'D' THEN 0 WHEN 'L' THEN 1 END AS Type
              FROM msdb.dbo.backupset b
              JOIN msdb.dbo.backupmediafamily mf 
                  ON b.media_set_id = mf.media_set_id
              WHERE
                  (@From IS NULL OR b.backup_finish_date >= @From) AND
                  (@To   IS NULL OR b.backup_finish_date <= @To) AND
                  b.type = @Type
              ORDER BY b.backup_finish_date DESC;",
            new SqlParameter("@Type", type == DatabaseBackupType.Full ? "D" : "L"),
            new SqlParameter("@From", (object)fromDateTime ?? DBNull.Value),
            new SqlParameter("@To", (object)toDateTime ?? DBNull.Value))
            .ToList();
        }
        catch (Exception ex)
        {
            throw new DatabaseBackupException("ListDatabaseBackupsException", ex);
        }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Check if full backup can be done
    /// Database have to be in 'FULL' or 'BULK_LOGGED' recovery mode
    /// </summary>
    /// <returns></returns>
    private bool DatabaseFullBackupCanBeDone()
    {
        var databaseName = _context.Database.GetDbConnection().Database;
        var result = _context.Database
            .SqlQueryRaw<int>(
                @"SELECT 1 AS Value 
                  FROM sys.databases d 
                  WHERE d.name = {0} 
                    AND d.recovery_model_desc IN ('FULL', 'BULK_LOGGED')",
                databaseName)
            .Single();

        return result > 0;
    }

    /// <summary>
    /// Check if transaction logs backup can be done
    /// Database have to be in 'FULL' or 'BULK_LOGGED' recovery mode and full backup needs to be performed first
    /// </summary>
    /// <returns></returns>
    private bool DatabaseLogsBackupCanBeDone()
    {
        var databaseName = _context.Database.GetDbConnection().Database;
        var result = _context.Database
            .SqlQueryRaw<int>(
                @"SELECT 1 AS Value
                FROM sys.databases d WHERE d.name = {0} 
                    AND d.recovery_model_desc IN ('FULL', 'BULK_LOGGED') 
                    AND EXISTS (SELECT 1 FROM msdb.dbo.backupset b
                WHERE b.database_name = {0} AND b.type = 'D')",
                databaseName)
            .Single();

        return result > 0;
    }

    #endregion
}
