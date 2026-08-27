using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HEAppE.DataAccessTier.Configuration;
using HEAppE.DomainObjects.Management;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Internal;

namespace HEAppE.DataAccessTier.Service;

internal class DatabaseBackupService : IDatabaseBackupService
{
    #region Constructors

    internal DatabaseBackupService(MiddlewareContext context, IVaultConnector vaultConnector)
        : this(context, vaultConnector, Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseBackupService>.Instance)
    {
    }

    internal DatabaseBackupService(MiddlewareContext context, IVaultConnector vaultConnector, ILogger logger)
    {
        _context = context;
        _vaultConnector = vaultConnector;
        _logger = logger;
    }

    #endregion

    #region Instances

    protected readonly MiddlewareContext _context;
    private readonly ILogger _logger;
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
            string backupConfsDirectory = Path.Combine(DatabaseFullBackupConfiguration.Current.LocalPath,
                $"confs_and_vault_backup_{dateTimeStamp}");


            #region Full database backup

            if (!DatabaseFullBackupCanBeDone())
                throw new DatabaseBackupException("FullBackupCantBeDone");

            if (!string.IsNullOrEmpty(DatabaseFullBackupConfiguration.Current.LocalPath))
            {
                Directory.CreateDirectory(DatabaseFullBackupConfiguration.Current.LocalPath);
            }

            var databaseName = _context.Database.GetDbConnection().Database;
            var backupFileName = $"{DatabaseFullBackupConfiguration.Current.BackupFileNamePrefix}_FULL_{dateTimeStamp}.bak";
            var backupPath = Path.Combine(DatabaseFullBackupConfiguration.Current.LocalPath, backupFileName);

#pragma warning disable EF1002 // Database name cannot be parameterized
            await _context.Database.ExecuteSqlRawAsync($"BACKUP DATABASE [{databaseName}] TO DISK = @path WITH INIT;", new SqlParameter("@path", backupPath));
#pragma warning restore EF1002

            // Copy to NAS
            if (!string.IsNullOrEmpty(DatabaseFullBackupConfiguration.Current.NASPath))
            {
                Directory.CreateDirectory(DatabaseFullBackupConfiguration.Current.NASPath);
                var nasFile = Path.Combine(DatabaseFullBackupConfiguration.Current.NASPath, backupFileName);
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

                    if (!string.IsNullOrEmpty(DatabaseFullBackupConfiguration.Current.NASPath))
                    {
                        string nasFolder = Path.Combine(DatabaseFullBackupConfiguration.Current.NASPath,
                            $"confs_backup_{dateTimeStamp}");
                        string nasDestFile = Path.Combine(nasFolder, relativePath);

                        Directory.CreateDirectory(Path.GetDirectoryName(nasDestFile)!);
                        File.Copy(fileInfo.FullName, nasDestFile, overwrite: true);
                    }
                }
            }
            else
            {
                _logger.LogWarning($"Configuration directory '{confsDirectory}' does not exist. Continuing without backing up configuration files.");
            }
            #endregion
            
            #region HashiCorp Vault backup

            _logger.LogInformation("Starting HashiCorp Vault snapshot as part of full backup.");
            try
            {
                byte[] vaultSnapshot = await _vaultConnector.CreateSnapshot();

                if (vaultSnapshot != null && vaultSnapshot.Length > 0)
                {
                    string vaultBackupFileName = $"vault_snapshot_{dateTimeStamp}.tar";
                    
                    string confsVaultPath = Path.Combine(backupConfsDirectory, vaultBackupFileName);
                    await File.WriteAllBytesAsync(confsVaultPath, vaultSnapshot);
                    
                    if (!string.IsNullOrEmpty(DatabaseFullBackupConfiguration.Current.NASPath))
                    {
                        string nasVaultPath = Path.Combine(DatabaseFullBackupConfiguration.Current.NASPath, vaultBackupFileName);
                        await File.WriteAllBytesAsync(nasVaultPath, vaultSnapshot);
                    }
                    _logger.LogDebug("Vault snapshot included in backup successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Vault backup failed, but continuing with DB backup: {ex.Message}");
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

            if (!string.IsNullOrEmpty(DatabaseTransactionLogBackupConfiguration.Current.LocalPath))
            {
                Directory.CreateDirectory(DatabaseTransactionLogBackupConfiguration.Current.LocalPath);
            }

            var databaseName = _context.Database.GetDbConnection().Database;
            var backupFileName = $"{DatabaseTransactionLogBackupConfiguration.Current.BackupFileNamePrefix}_LOGS_{DateTime.Now:yyyyMMddHHmm}.trn";
            var backupPath = Path.Combine(DatabaseTransactionLogBackupConfiguration.Current.LocalPath, backupFileName);

#pragma warning disable EF1002 // Database name cannot be parameterized
            _context.Database.ExecuteSqlRaw($"BACKUP LOG [{databaseName}] TO DISK = @path WITH INIT;", new SqlParameter("@path", backupPath));
#pragma warning restore EF1002

            // Copy to NAS
            if (!string.IsNullOrEmpty(DatabaseTransactionLogBackupConfiguration.Current.NASPath))
            {
                Directory.CreateDirectory(DatabaseTransactionLogBackupConfiguration.Current.NASPath);
                var nasFile = Path.Combine(DatabaseTransactionLogBackupConfiguration.Current.NASPath, backupFileName);
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
            var databaseName = _context.Database.GetDbConnection().Database;
            var backups = _context.Database.SqlQueryRaw<DatabaseBackup>(
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
                  b.database_name = @DbName AND
                  (@From IS NULL OR b.backup_finish_date >= @From) AND
                  (@To   IS NULL OR b.backup_finish_date <= @To) AND
                  b.type = @Type
              ORDER BY b.backup_finish_date DESC;",
            new SqlParameter("@DbName", databaseName),
            new SqlParameter("@Type", type == DatabaseBackupType.Full ? "D" : "L"),
            new SqlParameter("@From", (object)fromDateTime ?? DBNull.Value),
            new SqlParameter("@To", (object)toDateTime ?? DBNull.Value))
            .ToList();

            var folderPath = type == DatabaseBackupType.Full 
                ? DatabaseFullBackupConfiguration.Current.LocalPath 
                : DatabaseTransactionLogBackupConfiguration.Current.LocalPath;

            var nasPath = type == DatabaseBackupType.Full 
                ? DatabaseFullBackupConfiguration.Current.NASPath 
                : DatabaseTransactionLogBackupConfiguration.Current.NASPath;

            return backups.Where(b => 
            {
                if (string.IsNullOrEmpty(b.FileName)) return false;
                
                if (!string.IsNullOrEmpty(folderPath))
                {
                    var localFile = Path.Combine(folderPath, b.FileName);
                    if (File.Exists(localFile)) return true;
                }

                if (!string.IsNullOrEmpty(nasPath))
                {
                    var nasFile = Path.Combine(nasPath, b.FileName);
                    if (File.Exists(nasFile)) return true;
                }

                return false;
            })
            .GroupBy(b => b.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
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
        var backupPath = Path.Combine(DatabaseFullBackupConfiguration.Current.LocalPath, backupFileName);

        if (!File.Exists(backupPath))
        {
            var nasPath = Path.Combine(DatabaseFullBackupConfiguration.Current.NASPath ?? "", backupFileName);
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

    /// <summary>
    ///     Export migration package containing database and vault secrets.
    /// </summary>
    public async Task<byte[]> ExportMigrationPackage(string passphrase)
    {
        var keyToUse = string.IsNullOrEmpty(passphrase) ? MigrationSettings.EncryptionKey : passphrase;
        ValidatePassphraseStrength(keyToUse);

        try
        {

            // 1. Generate full database backup
            var databaseName = _context.Database.GetDbConnection().Database;
            var dateTimeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var backupFileName = $"MIGRATION_TEMP_{dateTimeStamp}.bak";
            var backupPath = Path.Combine(DatabaseFullBackupConfiguration.Current.LocalPath, backupFileName);

            _logger.LogInformation($"Creating temporary database backup for migration at: {backupPath}");
            Directory.CreateDirectory(DatabaseFullBackupConfiguration.Current.LocalPath);

#pragma warning disable EF1002
            await _context.Database.ExecuteSqlRawAsync($"BACKUP DATABASE [{databaseName}] TO DISK = @path WITH INIT;", new SqlParameter("@path", backupPath));
#pragma warning restore EF1002

            byte[] dbBackupBytes = await File.ReadAllBytesAsync(backupPath);
            try
            {
                File.Delete(backupPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to delete temporary backup file at: {backupPath}");
            }

            // 2. Export Vault secrets
            _logger.LogInformation("Exporting ClusterAuthenticationCredentials from database and HashiCorp Vault.");
            var credentialsList = await _context.ClusterAuthenticationCredentials.ToListAsync();
            var vaultSecrets = new List<ClusterProjectCredentialVaultPart>();
            
            foreach (var cred in credentialsList)
            {
                try
                {
                    var vaultData = await _vaultConnector.GetClusterAuthenticationCredentials(cred.Id);
                    if (vaultData != null && vaultData.Id > 0)
                    {
                        vaultSecrets.Add(vaultData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to retrieve Vault credentials for ID {cred.Id}");
                }
            }

            var vaultSecretsJson = JsonSerializer.Serialize(vaultSecrets, new JsonSerializerOptions 
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            byte[] vaultSecretsBytes = System.Text.Encoding.UTF8.GetBytes(vaultSecretsJson);

            // 3. Create ZIP archive in memory
            byte[] zipBytes;
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // Add database.bak
                    var dbEntry = archive.CreateEntry("database.bak");
                    using (var entryStream = dbEntry.Open())
                    {
                        await entryStream.WriteAsync(dbBackupBytes, 0, dbBackupBytes.Length);
                    }

                    // Add vault_secrets.json
                    var vaultEntry = archive.CreateEntry("vault_secrets.json");
                    using (var entryStream = vaultEntry.Open())
                    {
                        await entryStream.WriteAsync(vaultSecretsBytes, 0, vaultSecretsBytes.Length);
                    }
                }
                zipBytes = memoryStream.ToArray();
            }

            // 4. Encrypt zip bytes using AES-256-GCM
            _logger.LogInformation("Encrypting ZIP archive using AES-256-GCM.");
            byte[] encryptedBytes = EncryptBytes(zipBytes, keyToUse);
            
            return encryptedBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export migration package.");
            throw new DatabaseBackupException("ExportMigrationPackageException", ex);
        }
    }

    /// <summary>
    ///     Import migration package, restore database and restore Vault secrets.
    /// </summary>
    public async Task ImportMigrationPackage(Stream encryptedPackageStream, string passphrase)
    {
        var keyToUse = string.IsNullOrEmpty(passphrase) ? MigrationSettings.EncryptionKey : passphrase;
        ValidatePassphraseStrength(keyToUse);

        string tempBackupPath = null;
        try
        {

            // Read the encrypted package stream into memory
            byte[] encryptedBytes;
            using (var ms = new MemoryStream())
            {
                await encryptedPackageStream.CopyToAsync(ms);
                encryptedBytes = ms.ToArray();
            }

            // 1. Decrypt ZIP bytes
            _logger.LogInformation("Decrypting migration package using AES-256-GCM.");
            byte[] zipBytes = DecryptBytes(encryptedBytes, keyToUse);

            // 2. Extract files from ZIP archive
            byte[] dbBackupBytes = null;
            byte[] vaultSecretsBytes = null;

            using (var ms = new MemoryStream(zipBytes))
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                var dbEntry = archive.GetEntry("database.bak") ?? throw new Exception("Migration package is missing database.bak");
                var vaultEntry = archive.GetEntry("vault_secrets.json") ?? throw new Exception("Migration package is missing vault_secrets.json");

                using (var entryStream = dbEntry.Open())
                using (var dbMs = new MemoryStream())
                {
                    await entryStream.CopyToAsync(dbMs);
                    dbBackupBytes = dbMs.ToArray();
                }

                using (var entryStream = vaultEntry.Open())
                using (var vaultMs = new MemoryStream())
                {
                    await entryStream.CopyToAsync(vaultMs);
                    vaultSecretsBytes = vaultMs.ToArray();
                }
            }

            // Write temporary database backup file to LocalPath to restore from it
            Directory.CreateDirectory(DatabaseFullBackupConfiguration.Current.LocalPath);
            var dateTimeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            tempBackupPath = Path.Combine(DatabaseFullBackupConfiguration.Current.LocalPath, $"MIGRATION_RESTORE_TEMP_{dateTimeStamp}.bak");
            await File.WriteAllBytesAsync(tempBackupPath, dbBackupBytes);

            // 3. Restore database (similar to RestoreDatabase method)
            var databaseName = _context.Database.GetDbConnection().Database;
            var builder = new SqlConnectionStringBuilder(_context.Database.GetConnectionString()) { InitialCatalog = "master" };
            
            _logger.LogInformation($"Restoring database [{databaseName}] from temporary backup file: {tempBackupPath}");
            using (var connection = new SqlConnection(builder.ConnectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = $@"
                        ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        RESTORE DATABASE [{databaseName}] FROM DISK = @bp WITH REPLACE, RECOVERY;
                        ALTER DATABASE [{databaseName}] SET MULTI_USER;";
                    command.Parameters.AddWithValue("@bp", tempBackupPath);
                    await command.ExecuteNonQueryAsync();
                }
            }

            // Delete temporary backup file
            try
            {
                if (File.Exists(tempBackupPath))
                {
                    File.Delete(tempBackupPath);
                    tempBackupPath = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary restore backup file.");
            }

            // 4. Restore Vault secrets
            _logger.LogInformation("Parsing and restoring Vault secrets.");
            var vaultSecretsJson = System.Text.Encoding.UTF8.GetString(vaultSecretsBytes);
            var vaultSecrets = JsonSerializer.Deserialize<List<ClusterProjectCredentialVaultPart>>(vaultSecretsJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (vaultSecrets != null)
            {
                foreach (var vaultPart in vaultSecrets)
                {
                    _logger.LogDebug($"Writing Vault secret for Credential ID: {vaultPart.Id}");
                    await _vaultConnector.SetClusterAuthenticationCredentialsAsync(vaultPart);
                }
            }

            _logger.LogInformation("Migration package import completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import migration package.");
            throw new DatabaseBackupException("ImportMigrationPackageException", ex);
        }
        finally
        {
            // Clean up file if still exists
            try
            {
                if (!string.IsNullOrEmpty(tempBackupPath) && File.Exists(tempBackupPath))
                {
                    File.Delete(tempBackupPath);
                }
            }
            catch { }
        }
    }

    #endregion

    #region Private methods

    /// <summary>
    ///     Check if full backup can be done
    /// </summary>
    /// <returns></returns>
    private bool DatabaseFullBackupCanBeDone()
    {
        var databaseName = _context.Database.GetDbConnection().Database;
        var result = _context.Database
            .SqlQueryRaw<int?>(
                @"SELECT 1 AS Value 
                  FROM sys.databases d 
                  WHERE d.name = {0}",
                databaseName)
            .SingleOrDefault();

        return result.HasValue;
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

    private static byte[] EncryptBytes(byte[] plaintext, string passphrase)
    {
        byte[] salt = new byte[16];
        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(salt);
        RandomNumberGenerator.Fill(nonce);

        using var pbkdf2 = new Rfc2898DeriveBytes(passphrase, salt, 100000, HashAlgorithmName.SHA256);
        byte[] key = pbkdf2.GetBytes(32);

        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];

        using (var aesGcm = new AesGcm(key))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        byte[] result = new byte[salt.Length + nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
        Buffer.BlockCopy(nonce, 0, result, salt.Length, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, salt.Length + nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, salt.Length + nonce.Length + tag.Length, ciphertext.Length);

        return result;
    }

    private static byte[] DecryptBytes(byte[] encryptedData, string passphrase)
    {
        if (encryptedData.Length < 16 + 12 + 16)
        {
            throw new CryptographicException("Invalid encrypted data length.");
        }

        byte[] salt = new byte[16];
        byte[] nonce = new byte[12];
        byte[] tag = new byte[16];
        int ciphertextLength = encryptedData.Length - salt.Length - nonce.Length - tag.Length;
        byte[] ciphertext = new byte[ciphertextLength];

        Buffer.BlockCopy(encryptedData, 0, salt, 0, salt.Length);
        Buffer.BlockCopy(encryptedData, salt.Length, nonce, 0, nonce.Length);
        Buffer.BlockCopy(encryptedData, salt.Length + nonce.Length, tag, 0, tag.Length);
        Buffer.BlockCopy(encryptedData, salt.Length + nonce.Length + tag.Length, ciphertext, 0, ciphertextLength);

        using var pbkdf2 = new Rfc2898DeriveBytes(passphrase, salt, 100000, HashAlgorithmName.SHA256);
        byte[] key = pbkdf2.GetBytes(32);

        byte[] plaintext = new byte[ciphertextLength];
        using (var aesGcm = new AesGcm(key))
        {
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        return plaintext;
    }

    private static void ValidatePassphraseStrength(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new InputValidationException("Migration encryption key or passphrase must be configured or provided.");
        }

        if (key.Length < 12)
        {
            throw new InputValidationException("Migration passphrase/key must be at least 12 characters long.");
        }

        bool hasUpper = false;
        bool hasLower = false;
        bool hasDigit = false;
        bool hasSpecial = false;

        foreach (char c in key)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else if (!char.IsLetterOrDigit(c)) hasSpecial = true;
        }

        if (!hasUpper || !hasLower || !hasDigit || !hasSpecial)
        {
            throw new InputValidationException("Migration passphrase/key does not meet the security policy. It must contain at least: one uppercase letter, one lowercase letter, one digit, and one special character.");
        }
    }

    #endregion
}
