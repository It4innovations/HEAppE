using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.DataAccessTier;
using HEAppE.DataAccessTier.Configuration;
using HEAppE.DataAccessTier.Configuration.Shared;
using log4net;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;

namespace HEAppE.BackgroundThread.BackgroundServices;

internal class DatabaseTransactionLogBackupService : BackgroundService
{
    private readonly DatabaseTransactionLogBackupConfiguration _configuration;
    private readonly ILog _log;

    public DatabaseTransactionLogBackupService(DatabaseTransactionLogBackupConfiguration configuration)
    {
        _log = LogManager.GetLogger(GetType());
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                bool backupCanBeDone = _configuration.ScheduledBackupEnabled && await DatabaseLogsBackupCanBeDone();

                if (backupCanBeDone)
                {
                    await DoTransactionLogsBackupAsync();

                    ApplyRetentionPolicy(_configuration.LocalPath);
                    if (!string.IsNullOrEmpty(_configuration.NASPath))
                    {
                        ApplyRetentionPolicy(_configuration.NASPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("An error occured during execution of the DatabaseTransactionLogBackup background service: ", ex);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_configuration.BackupScheduleIntervalInMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private async Task<bool> DatabaseLogsBackupCanBeDone()
    {
        try
        {
            using var conn = new SqlConnection(MiddlewareContextSettings.ConnectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sys.databases d WHERE d.name = @db AND d.recovery_model_desc" +
                " IN ('FULL', 'BULK_LOGGED') AND EXISTS (SELECT 1 FROM msdb.dbo.backupset b WHERE b.database_name = @db " +
                "AND b.type = 'D')";
            cmd.Parameters.AddWithValue("@db", conn.Database);

            var result = await cmd.ExecuteScalarAsync();
            return result != null && (int)result > 0;
        }
        catch (Exception ex)
        {
            _log.Error("An error occured during check if database transaction logs backup can be performed: ", ex);
            return false;
        }
    }

    private async Task DoTransactionLogsBackupAsync()
    {
        try
        {
            using var conn = new SqlConnection(MiddlewareContextSettings.ConnectionString);
            await conn.OpenAsync();

            var backupFileName = $"{_configuration.BackupFileNamePrefix}_LOGS_{DateTime.Now:yyyyMMddHHmm}.trn";
            var backupPath = Path.Combine(_configuration.LocalPath, backupFileName);
            
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"BACKUP LOG [{conn.Database}] TO DISK = @path WITH INIT;";
            cmd.Parameters.AddWithValue("@path", backupPath);
            await cmd.ExecuteNonQueryAsync();

            _log.Info($"Transaction logs backup file was created to: {backupPath}");

            if (!string.IsNullOrEmpty(_configuration.NASPath))
            {
                var nasFile = Path.Combine(_configuration.NASPath, backupFileName);
                File.Copy(backupPath, nasFile, overwrite: true);
                _log.Info($"Transaction logs backup file was copied to NAS: {nasFile}");
            }
        }
        catch (Exception ex)
        {
            _log.Error("An error occured during execution of the transaction logs backup: ", ex);
        }
    }

    private void ApplyRetentionPolicy(string folder)
    {
        try
        {
            var files = Directory.GetFiles(folder, $"{_configuration.BackupFileNamePrefix}_LOGS_*.trn")
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
                        _log.Warn($"Failed to delete transaction logs backup '{item.File.FullName}'", ex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error("An error occured while removing older transaction logs backups: ", ex);
        }
    }

    private static DateTime? ParseDateFromFileName(string fileName)
    {
        try
        {
            var parts = fileName.Split('_');
            var datePart = parts[^1].Replace(".trn", "");
            return DateTime.ParseExact(datePart, "yyyyMMddHHmm", null);
        }
        catch
        {
            return null;
        }
    }

    private static BackupRetentionCategory? GetRetentionCategory(DateTime? date)
    {
        if (date == null) return null;
        if (date.Value.Day == 1) return BackupRetentionCategory.Monthly;
        if (date.Value.DayOfWeek == DayOfWeek.Sunday) return BackupRetentionCategory.Weekly;
        return BackupRetentionCategory.Daily;
    }

    private int GetNumberOfFilesToKeepByRetentionCategory(BackupRetentionCategory? category)
    {
        return category switch
        {
            BackupRetentionCategory.Daily => _configuration.RetentionPolicy.Daily,
            BackupRetentionCategory.Weekly => _configuration.RetentionPolicy.Weekly,
            BackupRetentionCategory.Monthly => _configuration.RetentionPolicy.Monthly,
            _ => 0
        };
    }
}