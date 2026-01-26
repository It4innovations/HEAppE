using HEAppE.DataAccessTier;
using HEAppE.DataAccessTier.Configuration;
using HEAppE.DataAccessTier.Configuration.Shared;
using log4net;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HEAppE.BackgroundThread.BackgroundServices;

/// <summary>
///     Automatic database backup background service.
/// </summary>
internal class DatabaseFullBackupBackgroundService : BackgroundService
{
    private readonly TimeSpan _scheduledTime = TimeSpan.Parse(DatabaseFullBackupConfiguration.ScheduledRuntime, new CultureInfo("en-US"));
    protected readonly ILog _log;

    public DatabaseFullBackupBackgroundService()
    {
        _log = LogManager.GetLogger(GetType());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        var backupCanBeDone = DatabaseFullBackupConfiguration.ScheduledBackupEnabled && await DatabaseFullBackupCanBeDone();

        while (backupCanBeDone && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                if (now.TimeOfDay >= _scheduledTime && now.TimeOfDay < _scheduledTime.Add(TimeSpan.FromMinutes(2)))
                {
                    // backup database
                    await DoFullBackupAsync();

                    // do retention of older backups
                    ApplyRetentionPolicy(DatabaseFullBackupConfiguration.LocalPath);
                    if (!string.IsNullOrEmpty(DatabaseFullBackupConfiguration.NASPath))
                    {
                        ApplyRetentionPolicy(DatabaseFullBackupConfiguration.NASPath);
                    }

                    // delay to another day
                    var tomorrow = DateTime.Today.AddDays(1).Add(_scheduledTime);
                    var delay = tomorrow - DateTime.Now;
                    await Task.Delay(delay, stoppingToken);
                }
                else
                {
                    // check again after minute
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _log.Error("An error occured during execution of the DatabaseFullBackup background service: ", ex);
            }
        }
    }

    /// <summary>
    /// Check if full backup can be done
    /// Database have to be in 'FULL' or 'BULK_LOGGED' recovery mode
    /// </summary>
    /// <returns></returns>
    private async Task<bool> DatabaseFullBackupCanBeDone()
    {
        try
        {
            using var conn = new SqlConnection(MiddlewareContextSettings.ConnectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT 1 FROM sys.databases d WHERE d.name = '{conn.Database}' " +
                $"AND d.recovery_model_desc IN ('FULL', 'BULK_LOGGED')";

            var result = await cmd.ExecuteScalarAsync();
            var canBeDone = result != null && (int)result > 0;

            if (!canBeDone)
                _log.Info("Database full backup can't be done. Database needs to be in 'FULL' or 'BULK_LOGGED' recovery model.");

            return canBeDone;
        }
        catch (Exception ex)
        {
            _log.Error("An error occured during check if full database backup can be performed in DatabaseFullBackup background service. ", ex);
            return false;
        }
    }

    /// <summary>
    /// Backup database and copy backup file to NAS.
    /// </summary>
    /// <returns></returns>
    private async Task DoFullBackupAsync()
    {
        try
        {
            using var conn = new SqlConnection(MiddlewareContextSettings.ConnectionString);
            await conn.OpenAsync();

            var backupFileName = $"{DatabaseFullBackupConfiguration.BackupFileNamePrefix}_FULL_{DateTime.Now:yyyyMMddHHmm}.bak";
            var backupPath = Path.Combine(DatabaseFullBackupConfiguration.LocalPath, backupFileName);
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"BACKUP DATABASE [{conn.Database}] TO DISK = '{backupPath}' WITH INIT;";
            await cmd.ExecuteNonQueryAsync();

            _log.Info(string.Format("Database backup file was created to: {0}", backupPath));

            // Copy to NAS
            if (!string.IsNullOrEmpty(DatabaseFullBackupConfiguration.NASPath))
            {
                var nasFile = Path.Combine(DatabaseFullBackupConfiguration.NASPath, backupFileName);
                File.Copy(backupPath, nasFile, overwrite: true);
                _log.Info(string.Format("Database backup file was copied to NAS: {0}", nasFile));
            }
        }
        catch (Exception ex)
        {
            _log.Error("An error occured during execution of the database backup. ", ex);
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
            var files = Directory.GetFiles(folder, $"{DatabaseFullBackupConfiguration.BackupFileNamePrefix}_FULL_*.bak")
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
                        _log.Warn(string.Format("Failed to delete database backup file '{0}'", item.File.FullName), ex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error("An error occured while removing older database backup files in background service: ", ex);
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
            // name is in format '{DatabaseName}_FULL_yyyyMMddHHmm.bak'
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
            BackupRetentionCategory.Daily => DatabaseFullBackupConfiguration.RetentionPolicy.Daily,
            BackupRetentionCategory.Weekly => DatabaseFullBackupConfiguration.RetentionPolicy.Weekly,
            BackupRetentionCategory.Monthly => DatabaseFullBackupConfiguration.RetentionPolicy.Monthly,
            _ => 0
        };
    }
}