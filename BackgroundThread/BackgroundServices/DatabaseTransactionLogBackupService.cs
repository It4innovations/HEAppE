using HEAppE.DataAccessTier;
using HEAppE.DataAccessTier.Configuration;
using HEAppE.DataAccessTier.Configuration.Shared;
using log4net;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HEAppE.BackgroundThread.BackgroundServices;

internal class DatabaseTransactionLogBackupService : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(DatabaseTransactionLogBackupConfiguration.BackupScheduleIntervalInMinutes);
    protected readonly ILog _log;

    public DatabaseTransactionLogBackupService()
    {
        _log = LogManager.GetLogger(GetType());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var backupCanBeDone = DatabaseTransactionLogBackupConfiguration.ScheduledBackupEnabled && await DatabaseLogsBackupCanBeDone();
        if(true)
        {
            _log.Info("Database transaction logs backup background service is disabled or can't be performed. Exiting the service.");
            return;
        }
        while (backupCanBeDone && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                // backup database transaction logs
                await DoTransactionLogsBackupAsync();

                // do retention of older backups
                ApplyRetentionPolicy(DatabaseTransactionLogBackupConfiguration.LocalPath);
                if (!string.IsNullOrEmpty(DatabaseTransactionLogBackupConfiguration.NASPath))
                {
                    ApplyRetentionPolicy(DatabaseTransactionLogBackupConfiguration.NASPath);
                }
            }
            catch (Exception ex)
            {
                _log.Error("An error occured during execution of the DatabaseTransactionLogBackup background service: ", ex);
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    /// <summary>
    /// Check if transaction logs backup can be done
    /// Database have to be in 'FULL' or 'BULK_LOGGED' recovery mode and full backup needs to be performed first
    /// </summary>
    /// <returns></returns>
    private async Task<bool> DatabaseLogsBackupCanBeDone()
    {
        try
        {
            using var conn = new SqlConnection(MiddlewareContextSettings.ConnectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT 1 FROM sys.databases d WHERE d.name = '{conn.Database}' AND d.recovery_model_desc" +
                $" IN ('FULL', 'BULK_LOGGED') AND EXISTS (SELECT 1 FROM msdb.dbo.backupset b WHERE b.database_name = '{conn.Database}' " +
                $"AND b.type = 'D')";

            var result = await cmd.ExecuteScalarAsync();
            var canBeDone = result != null && (int)result > 0;

            if (!canBeDone)
                _log.Info("Database transaction logs backup can't be done. There needs to exist full backup and database need to be in 'FULL' or 'BULK_LOGGED' recovery model.");

            return canBeDone;
        }
        catch(Exception ex)
        {
            _log.Error("An error occured during check if database transaction logs backup can be performed in DatabaseTransactionLogBackup background service. ", ex);
            return false;
        }
    }

    /// <summary>
    /// Backup transaction database logs and copy backup file to NAS.
    /// </summary>
    /// <returns></returns>
    private async Task DoTransactionLogsBackupAsync()
    {
        try
        {
            using var conn = new SqlConnection(MiddlewareContextSettings.ConnectionString);
            await conn.OpenAsync();

            var backupFileName = $"{DatabaseTransactionLogBackupConfiguration.BackupFileNamePrefix}_LOGS_{DateTime.Now:yyyyMMddHHmm}.trn";
            var backupPath = Path.Combine(DatabaseTransactionLogBackupConfiguration.LocalPath, backupFileName);
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"BACKUP LOG [{conn.Database}] TO DISK = '{backupPath}' WITH INIT;";
            await cmd.ExecuteNonQueryAsync();

            _log.Info(string.Format("Transaction logs backup file was created to: {0}", backupPath));

            // Copy to NAS
            if (!string.IsNullOrEmpty(DatabaseTransactionLogBackupConfiguration.NASPath))
            {
                var nasFile = Path.Combine(DatabaseTransactionLogBackupConfiguration.NASPath, backupFileName);
                File.Copy(backupPath, nasFile, overwrite: true);
                _log.Info(string.Format("Transaction logs backup file was copied to NAS: {0}", nasFile));
            }
        }
        catch (Exception ex)
        {
            _log.Error("An error occured during execution of the transaction logs backup. ", ex);
        }
    }

    /// <summary>
    /// Apply retention policy of backup files in folder.
    /// </summary>
    /// <param name="folder"></param>
    private void ApplyRetentionPolicy(string folder)
    {
        try
        {
            var files = Directory.GetFiles(folder, $"{DatabaseTransactionLogBackupConfiguration.BackupFileNamePrefix}_LOGS_*.trn")
                             .Select(f => new FileInfo(f))
                             .OrderByDescending(f => f.CreationTime)
                             .ToList();

            var grouped = files.Select(f => new
            {
                File = f,
                Date = ParseDateFromFileName(f.Name),
                RetentionCategory = GetRetentionCategory(ParseDateFromFileName(f.Name))
            })
                .Where(x => x.Date != null)
                .GroupBy(x => x.RetentionCategory);

            foreach (var group in grouped)
            {
                int keep = GetNumberOfFilesToKeepByRetentionCategory(group.Key);
                foreach (var item in group.Skip(keep))
                {
                    try
                    {
                        item.File.Delete();
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(string.Format("Failed to delete transaction logs backup '{0}'", item.File.FullName), ex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error("An error occured while removing older transaction logs backups in background service: ", ex);
        }
    }

    /// <summary>
    /// Parse date from backup file name.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private static DateTime? ParseDateFromFileName(string fileName)
    {
        try
        {
            // name is in format '{DatabaseName}_LOGS_yyyyMMddHHmm.bak'
            var parts = fileName.Split('_');
            var datePart = parts[^1].Replace(".bak", ""); // yyyyMMddHHmm
            return DateTime.ParseExact(datePart, "yyyyMMddHHmm", null);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get backup retention category based on backup file creation date.
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    private static BackupRetentionCategory? GetRetentionCategory(DateTime? date)
    {
        if (date == null) return null;
        if (date.Value.Day == 1) return BackupRetentionCategory.Monthly;
        if (date.Value.DayOfWeek == DayOfWeek.Sunday) return BackupRetentionCategory.Weekly;
        return BackupRetentionCategory.Daily;
    }

    /// <summary>
    /// Get number of files to keep by backup retention category. 
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    private static int GetNumberOfFilesToKeepByRetentionCategory(BackupRetentionCategory? category)
    {
        return category switch
        {
            BackupRetentionCategory.Daily => DatabaseTransactionLogBackupConfiguration.RetentionPolicy.Daily,
            BackupRetentionCategory.Weekly => DatabaseTransactionLogBackupConfiguration.RetentionPolicy.Weekly,
            BackupRetentionCategory.Monthly => DatabaseTransactionLogBackupConfiguration.RetentionPolicy.Monthly,
            _ => 0
        };
    }
}
