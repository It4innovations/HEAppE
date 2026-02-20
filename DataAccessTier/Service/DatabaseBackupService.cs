using HEAppE.DataAccessTier.Configuration;
using HEAppE.DomainObjects.Management;
using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Internal;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using log4net;

namespace HEAppE.DataAccessTier.Service;

internal class DatabaseBackupService : IDatabaseBackupService
{
    #region Constructors

    internal DatabaseBackupService(MiddlewareContext context, IVaultConnector vaultConnector)
    {
        _context = context;
        _vaultConnector = vaultConnector;
        _log = LogManager.GetLogger(nameof(DatabaseBackupService));
        
    }

    #endregion

    #region Instances

    protected readonly MiddlewareContext _context;
    private readonly ILog _log;
    private readonly IVaultConnector _vaultConnector;

    #endregion

    #region Methods

    /// <summary>
    ///     Full backup database.
    /// </summary>
    /// <returns>Path to created backup.</returns>
    public async Task<string> BackupDatabase()
    {
        try
        {
            string dateTimeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string confsDirectory = "/opt/heappe/confs/";
            string backupConfsDirectory = Path.Combine(DatabaseFullBackupConfiguration.LocalPath,
                $"confs_and_vault_backup_{dateTimeStamp}");


            #region Full database backup

            if (!DatabaseFullBackupCanBeDone())
                throw new DatabaseBackupException("FullBackupCantBeDone");

            var databaseName = _context.Database.GetDbConnection().Database;
            var backupFileName = $"{DatabaseFullBackupConfiguration.BackupFileNamePrefix}_FULL_{dateTimeStamp}.bak";
            var backupPath = Path.Combine(DatabaseFullBackupConfiguration.LocalPath, backupFileName);

            string sql = $"BACKUP DATABASE [{databaseName}] TO DISK = N'{backupPath}' WITH INIT;";
            await _context.Database.ExecuteSqlRawAsync(sql);

            // Copy to NAS
            if (!string.IsNullOrEmpty(DatabaseFullBackupConfiguration.NASPath))
            {
                var nasFile = Path.Combine(DatabaseFullBackupConfiguration.NASPath, backupFileName);
                File.Copy(backupPath, nasFile, overwrite: true);
            }

            #endregion


            #region Confs backup

            if (Directory.Exists(confsDirectory))
            {
                var dirInfo = new DirectoryInfo(confsDirectory);
                var allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories)
                    .Where(f => (f.Attributes & FileAttributes.Hidden) == 0);

                foreach (var fileInfo in allFiles)
                {
                    string relativePath = Path.GetRelativePath(dirInfo.FullName, fileInfo.FullName);

                    string localDestFile = Path.Combine(backupConfsDirectory, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(localDestFile)!);
                    File.Copy(fileInfo.FullName, localDestFile, overwrite: true);

                    if (!string.IsNullOrEmpty(DatabaseFullBackupConfiguration.NASPath))
                    {
                        string nasFolder = Path.Combine(DatabaseFullBackupConfiguration.NASPath,
                            $"confs_backup_{dateTimeStamp}");
                        string nasDestFile = Path.Combine(nasFolder, relativePath);

                        Directory.CreateDirectory(Path.GetDirectoryName(nasDestFile)!);
                        File.Copy(fileInfo.FullName, nasDestFile, overwrite: true);
                    }
                }
            }
            else
            {
                _log.Warn($"Configuration directory '{confsDirectory}' does not exist. Continuing without backing up configuration files.");
            }
            #endregion
            
            #region HashiCorp Vault backup

            _log.Info("Starting HashiCorp Vault snapshot as part of full backup.");
            try
            {
                byte[] vaultSnapshot = await _vaultConnector.CreateSnapshot();

                if (vaultSnapshot != null && vaultSnapshot.Length > 0)
                {
                    string vaultBackupFileName = $"vault_snapshot_{dateTimeStamp}.tar";
                    
                    string confsVaultPath = Path.Combine(backupConfsDirectory, vaultBackupFileName);
                    await File.WriteAllBytesAsync(confsVaultPath, vaultSnapshot);
                    
                    if (!string.IsNullOrEmpty(DatabaseFullBackupConfiguration.NASPath))
                    {
                        string nasVaultPath = Path.Combine(DatabaseFullBackupConfiguration.NASPath, vaultBackupFileName);
                        await File.WriteAllBytesAsync(nasVaultPath, vaultSnapshot);
                    }
                    _log.Debug("Vault snapshot included in backup successfully.");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Vault backup failed, but continuing with DB backup: {ex.Message}");
            }

            #endregion
            
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

    /// <summary>
    ///     Restore database from speficied backup file name.
    /// </summary>
    /// <param name="backupFileName"></param>
    /// <param name="includeLogs"></param>
   public void RestoreDatabase(string backupFileName, bool includeLogs)
    {
        var backupPath = Path.Combine(DatabaseFullBackupConfiguration.LocalPath, backupFileName);

        if (!File.Exists(backupPath))
        {
            var nasPath = Path.Combine(DatabaseFullBackupConfiguration.NASPath ?? "", backupFileName);
            if (File.Exists(nasPath)) backupPath = nasPath;
            else throw new DatabaseRestoreExternalException("BackupFileNameNotFoundException", backupFileName);
        }

        var builder = new SqlConnectionStringBuilder(_context.Database.GetConnectionString()) { InitialCatalog = "master" };
        var databaseName = _context.Database.GetDbConnection().Database;

        using var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandTimeout = 0;

        try
        {
            var logFiles = new List<string>();

            if (includeLogs)
            {
                command.CommandText = @"
                    SELECT TOP 1 b.backup_finish_date 
                    FROM msdb.dbo.backupset b
                    JOIN msdb.dbo.backupmediafamily mf ON b.media_set_id = mf.media_set_id
                    WHERE mf.physical_device_name LIKE @path
                    ORDER BY b.backup_finish_date DESC";
                command.Parameters.AddWithValue("@path", "%" + backupFileName);
                var finishDate = command.ExecuteScalar();
                command.Parameters.Clear();

                if (finishDate != null)
                {
                    command.CommandText = @"
                        SELECT mf.physical_device_name
                        FROM msdb.dbo.backupset b
                        JOIN msdb.dbo.backupmediafamily mf ON b.media_set_id = mf.media_set_id
                        WHERE b.database_name = @db AND b.type = 'L' AND b.backup_finish_date > @fullTime
                        ORDER BY b.backup_finish_date ASC;";
                    command.Parameters.AddWithValue("@db", databaseName);
                    command.Parameters.AddWithValue("@fullTime", (DateTime)finishDate);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) logFiles.Add(reader.GetString(0));
                    }
                    command.Parameters.Clear();
                }
            }

            command.CommandText = $@"
                ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                RESTORE DATABASE [{databaseName}] FROM DISK = @bp WITH REPLACE, NORECOVERY;";
            command.Parameters.AddWithValue("@bp", backupPath);
            command.ExecuteNonQuery();
            command.Parameters.Clear();

            bool recoveryPerformed = false;
            if (includeLogs && logFiles.Count > 0)
            {
                foreach (var logFile in logFiles)
                {
                    command.CommandText = "DECLARE @exists INT; EXEC master.dbo.xp_fileexist @path, @exists OUTPUT; SELECT @exists;";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@path", logFile);
                    var fileExists = (int)command.ExecuteScalar();

                    if (fileExists == 1)
                    {
                        try
                        {
                            bool isLast = (logFile == logFiles.Last());
                            command.CommandText = $"RESTORE LOG [{databaseName}] FROM DISK = @lp WITH {(isLast ? "RECOVERY" : "NORECOVERY")};";
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@lp", logFile);
                            command.ExecuteNonQuery();
                            
                            if (isLast) recoveryPerformed = true;
                        }
                        catch (SqlException ex) when (ex.Number == 4305)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (!recoveryPerformed)
            {
                command.CommandText = $"RESTORE DATABASE [{databaseName}] WITH RECOVERY;";
                command.Parameters.Clear();
                command.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            try
            {
                command.Parameters.Clear();
                command.CommandText = $"RESTORE DATABASE [{databaseName}] WITH RECOVERY;";
                command.ExecuteNonQuery();
            }
            catch { }

            throw new DatabaseRestoreException("RestoreDatabaseException", ex);
        }
        finally
        {
            try
            {
                command.Parameters.Clear();
                command.CommandText = $@"
                    IF EXISTS (SELECT 1 FROM sys.databases WHERE name = @db AND state = 0)
                    BEGIN
                        ALTER DATABASE [{databaseName}] SET MULTI_USER;
                    END";
                command.Parameters.AddWithValue("@db", databaseName);
                command.ExecuteNonQuery();
            }
            catch { }
        }
    }

    #endregion

    #region Private methods

    /// <summary>
    ///     Check if full backup can be done
    ///     Database have to be in 'FULL' or 'BULK_LOGGED' recovery mode
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
    ///     Check if transaction logs backup can be done
    ///     Database have to be in 'FULL' or 'BULK_LOGGED' recovery mode and full backup needs to be performed first
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
