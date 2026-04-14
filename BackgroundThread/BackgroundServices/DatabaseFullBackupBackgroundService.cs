using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.DataAccessTier;
using HEAppE.DataAccessTier.Configuration;
using HEAppE.DataAccessTier.Configuration.Shared;
using HEAppE.DataAccessTier.Vault;
using log4net;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;

namespace HEAppE.BackgroundThread.BackgroundServices;

internal class DatabaseFullBackupBackgroundService : BackgroundService
{
    private readonly ILog _log;
    private readonly VaultConnector _vaultConnector = new VaultConnector();
    private readonly DatabaseFullBackupConfiguration _configuration;

    public DatabaseFullBackupBackgroundService(DatabaseFullBackupConfiguration configuration)
    {
        _log = LogManager.GetLogger(GetType());
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                bool backupCanBeDone = _configuration.ScheduledBackupEnabled && await DatabaseFullBackupCanBeDone();
                
                if (backupCanBeDone)
                {
                    DateTime now = DateTime.Now;
                    TimeSpan scheduledTime = TimeSpan.Parse(_configuration.ScheduledRuntime, new CultureInfo("en-US"));

                    if (now.TimeOfDay >= scheduledTime && now.TimeOfDay < scheduledTime.Add(TimeSpan.FromMinutes(2)))
                    {
                        await DoFullBackupAsync();

                        ApplyRetentionPolicy(_configuration.LocalPath);
                        if (!string.IsNullOrEmpty(_configuration.NASPath))
                        {
                            ApplyRetentionPolicy(_configuration.NASPath);
                        }

                        DateTime tomorrow = DateTime.Today.AddDays(1).Add(scheduledTime);
                        await Task.Delay(tomorrow - DateTime.Now, stoppingToken);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("An error occured during execution of the DatabaseFullBackup background service: ", ex);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private async Task<bool> DatabaseFullBackupCanBeDone()
    {
        try
        {
            using var conn = new SqlConnection(MiddlewareContextSettings.ConnectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sys.databases d WHERE d.name = @db " +
                "AND d.recovery_model_desc IN ('FULL', 'BULK_LOGGED')";
            cmd.Parameters.AddWithValue("@db", conn.Database);

            var result = await cmd.ExecuteScalarAsync();
            return result != null && (int)result > 0;
        }
        catch (Exception ex)
        {
            _log.Error("An error occured during check if full database backup can be performed: ", ex);
            return false;
        }
    }

    private async Task DoFullBackupAsync()
    {
        try
        {
            string dateTimeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string confsDirectory = "/opt/heappe/confs/";
            string backupConfsDirectory = Path.Combine(_configuration.LocalPath, $"confs_and_vault_backup_{dateTimeStamp}");
            
            using var conn = new SqlConnection(MiddlewareContextSettings.ConnectionString);
            await conn.OpenAsync();

            string backupFileName = $"{_configuration.BackupFileNamePrefix}_FULL_{DateTime.Now:yyyyMMddHHmm}.bak";
            string backupPath = Path.Combine(_configuration.LocalPath, backupFileName);
            
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"BACKUP DATABASE [{conn.Database}] TO DISK = @path WITH INIT;";
            cmd.Parameters.AddWithValue("@path", backupPath);
            await cmd.ExecuteNonQueryAsync();

            _log.Info($"Database backup file was created to: {backupPath}");

            if (!string.IsNullOrEmpty(_configuration.NASPath))
            {
                string nasFile = Path.Combine(_configuration.NASPath, backupFileName);
                File.Copy(backupPath, nasFile, overwrite: true);
                _log.Info($"Database backup file was copied to NAS: {nasFile}");
            }
            
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

                    if (!string.IsNullOrEmpty(_configuration.NASPath))
                    {
                        string nasFolder = Path.Combine(_configuration.NASPath, $"confs_backup_{dateTimeStamp}");
                        string nasDestFile = Path.Combine(nasFolder, relativePath);

                        Directory.CreateDirectory(Path.GetDirectoryName(nasDestFile)!);
                        File.Copy(fileInfo.FullName, nasDestFile, overwrite: true);
                    }
                }
            }

            try
            {
                byte[] vaultSnapshot = await _vaultConnector.CreateSnapshot();
                if (vaultSnapshot != null && vaultSnapshot.Length > 0)
                {
                    string vaultBackupFileName = $"vault_snapshot_{dateTimeStamp}.tar";
                    await File.WriteAllBytesAsync(Path.Combine(_configuration.LocalPath, vaultBackupFileName), vaultSnapshot);
                    
                    Directory.CreateDirectory(backupConfsDirectory);
                    await File.WriteAllBytesAsync(Path.Combine(backupConfsDirectory, vaultBackupFileName), vaultSnapshot);
                    
                    if (!string.IsNullOrEmpty(_configuration.NASPath))
                    {
                        await File.WriteAllBytesAsync(Path.Combine(_configuration.NASPath, vaultBackupFileName), vaultSnapshot);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Vault backup failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _log.Error("An error occured during execution of the database backup: ", ex);
        }
    }

    private void ApplyRetentionPolicy(string folder)
    {
        try
        {
            var files = Directory.GetFiles(folder, $"{_configuration.BackupFileNamePrefix}_FULL_*.bak")
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
                    try { item.File.Delete(); }
                    catch (Exception ex) { _log.Warn($"Failed to delete backup file '{item.File.FullName}'", ex); }
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error("An error occured while removing older database backup files: ", ex);
        }
    }

    private static DateTime? ParseDateFromFileName(string fileName)
    {
        try
        {
            var parts = fileName.Split('_');
            var datePart = parts[^1].Replace(".bak", "");
            return DateTime.ParseExact(datePart, "yyyyMMddHHmm", null);
        }
        catch { return null; }
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