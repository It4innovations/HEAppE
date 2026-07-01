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
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HEAppE.BackgroundThread.BackgroundServices;

internal class DatabaseFullBackupBackgroundService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly VaultConnector _vaultConnector;
    private readonly DatabaseFullBackupConfiguration _configuration;

    public DatabaseFullBackupBackgroundService(ILoggerFactory loggerFactory, DatabaseFullBackupConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger("HEAppE.BackgroundThread.BackgroundServices.DatabaseFullBackupBackgroundService");
        _vaultConnector = new VaultConnector(_logger);
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        _logger.LogInformation("DatabaseFullBackupBackgroundService started.");

        // Run initial retention policy check on startup to clean up obsolete/old backups
        try
        {
            if (Directory.Exists(_configuration.LocalPath))
            {
                ApplyRetentionPolicy(_configuration.LocalPath);
            }
            if (!string.IsNullOrEmpty(_configuration.NASPath) && Directory.Exists(_configuration.NASPath))
            {
                ApplyRetentionPolicy(_configuration.NASPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during initial database full backup retention cleanup on startup: ");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_configuration.ScheduledBackupEnabled)
                {
                    _logger.LogDebug("Database full backup is disabled in configuration.");
                }
                else
                {
                    DateTime now = DateTime.Now;
                    TimeSpan scheduledTime = TimeSpan.Parse(_configuration.ScheduledRuntime, new CultureInfo("en-US"));
                    _logger.LogDebug($"Checking backup schedule. Current time of day: {now.TimeOfDay}, scheduled time: {scheduledTime}.");

                    if (now.TimeOfDay >= scheduledTime)
                    {
                        _logger.LogDebug("Current time is past scheduled runtime. Checking if backup is needed.");
                        if (!BackupForTodayExists(_configuration.LocalPath))
                        {
                            _logger.LogDebug("No backup found for today. Checking if backup can be performed.");
                            if (await DatabaseFullBackupCanBeDone())
                            {
                                _logger.LogInformation("Starting scheduled database full backup...");
                                await DoFullBackupAsync();

                                ApplyRetentionPolicy(_configuration.LocalPath);
                                if (!string.IsNullOrEmpty(_configuration.NASPath))
                                {
                                    ApplyRetentionPolicy(_configuration.NASPath);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Database full backup cannot be performed (DatabaseFullBackupCanBeDone returned false).");
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Database full backup for today already exists. Skipping execution.");
                        }

                        DateTime tomorrow = DateTime.Today.AddDays(1).Add(scheduledTime);
                        TimeSpan delay = tomorrow - DateTime.Now;
                        if (delay > TimeSpan.Zero)
                        {
                            _logger.LogInformation($"Next database full backup scheduled in {delay.TotalHours:F2} hours at {tomorrow:yyyy-MM-dd HH:mm:ss}.");
                            await Task.Delay(delay, stoppingToken);
                            continue;
                        }
                    }
                    else
                    {
                        _logger.LogDebug($"Current time {now.TimeOfDay} is before scheduled time {scheduledTime}. Will check again in 1 minute.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during execution of the DatabaseFullBackup background service: ");
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

    private bool BackupForTodayExists(string folder)
    {
        try
        {
            _logger.LogDebug($"Checking if backup for today exists in folder: '{folder}'");
            if (!Directory.Exists(folder))
            {
                _logger.LogDebug($"Folder '{folder}' does not exist.");
                return false;
            }
            string dateStr = DateTime.Now.ToString("yyyyMMdd");
            string searchPattern = $"{_configuration.BackupFileNamePrefix}_FULL_{dateStr}*.bak";
            _logger.LogDebug($"Searching for files matching pattern: '{searchPattern}'");
            var files = Directory.GetFiles(folder, searchPattern);
            bool exists = files.Any();
            _logger.LogDebug($"Backup files found: {files.Length}. Today's backup exists: {exists}");
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to check if backup for today exists in '{folder}': ");
            return false;
        }
    }

    private async Task<bool> DatabaseFullBackupCanBeDone()
    {
        try
        {
            _logger.LogDebug($"Checking if full database backup can be performed. ConnectionString: '{MiddlewareContextSettings.ConnectionString}'");
            using var conn = new SqlConnection(MiddlewareContextSettings.ConnectionString);
            await conn.OpenAsync();
            _logger.LogDebug($"Opened database connection successfully. Database: '{conn.Database}'");

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sys.databases d WHERE d.name = @db";
            cmd.Parameters.AddWithValue("@db", conn.Database);

            var result = await cmd.ExecuteScalarAsync();
            bool canBeDone = result != null && (int)result > 0;
            _logger.LogDebug($"Database existence check in sys.databases returned: {canBeDone}");
            return canBeDone;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured during check if full database backup can be performed: ");
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

            _logger.LogInformation($"Database backup file was created to: {backupPath}");

            if (!string.IsNullOrEmpty(_configuration.NASPath))
            {
                string nasFile = Path.Combine(_configuration.NASPath, backupFileName);
                File.Copy(backupPath, nasFile, overwrite: true);
                _logger.LogInformation($"Database backup file was copied to NAS: {nasFile}");
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
                _logger.LogError(ex, $"Vault backup failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured during execution of the database backup: ");
        }
    }

    private void ApplyRetentionPolicy(string folder)
    {
        try
        {
            var grouped = Directory.GetFiles(folder, $"{_configuration.BackupFileNamePrefix}_FULL_*.bak")
                             .Select(f => new FileInfo(f))
                             .Select(f => new { File = (FileSystemInfo)f, IsDirectory = false })
                             .Concat(Directory.GetDirectories(folder, "confs_*backup_*")
                                     .Select(d => new DirectoryInfo(d))
                                     .Select(d => new { File = (FileSystemInfo)d, IsDirectory = true }))
                             .Concat(Directory.GetFiles(folder, "vault_snapshot_*.tar")
                                     .Select(f => new FileInfo(f))
                                     .Select(f => new { File = (FileSystemInfo)f, IsDirectory = false }))
                             .Select(x => new
                             {
                                 x.File,
                                 x.IsDirectory,
                                 Date = ParseDateFromFileName(x.File.Name),
                                 RetentionCategory = GetRetentionCategory(ParseDateFromFileName(x.File.Name))
                             })
                             .Where(x => x.Date != null)
                             .OrderByDescending(x => x.Date)
                             .GroupBy(x => x.RetentionCategory);

            foreach (var group in grouped)
            {
                int keep = GetNumberOfFilesToKeepByRetentionCategory(group.Key);
                foreach (var item in group.Skip(keep))
                {
                    try 
                    { 
                        if (item.IsDirectory) Directory.Delete(item.File.FullName, true);
                        else item.File.Delete(); 
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, $"Failed to delete backup {(item.IsDirectory ? "directory" : "file")} '{item.File.FullName}'"); }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured while removing older database backup files. ");
        }
    }

    private static DateTime? ParseDateFromFileName(string fileName)
    {
        try
        {
            var parts = Path.GetFileNameWithoutExtension(fileName).Split('_');
            var datePart = parts[^1];
            if (datePart.Length == 14) return DateTime.ParseExact(datePart, "yyyyMMddHHmmss", null);
            if (datePart.Length == 12) return DateTime.ParseExact(datePart, "yyyyMMddHHmm", null);
            return null;
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