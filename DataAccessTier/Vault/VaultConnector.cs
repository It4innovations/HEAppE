using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HEAppE.DataAccessTier.Vault.Settings;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.Vault;

public class VaultConnector : IVaultConnector
{
    private static readonly TimeSpan _credentialsCacheTtl = TimeSpan.FromMinutes(30);
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<long, (Lazy<Task<ClusterProjectCredentialVaultPart>> Task, DateTime ExpiresAtUtc)> _credentialsCache =
        new();

    private readonly HttpClient _httpClient;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<long, (Lazy<Task<ClusterProjectCredentialVaultPart>> Task, DateTime ExpiresAtUtc)> _cache;
    private readonly string _clusterAuthenticationCredentialsPath = VaultConnectorSettings.ClusterAuthenticationCredentialsPath;
    private readonly ILogger _logger;

    public VaultConnector() : this(Microsoft.Extensions.Logging.Abstractions.NullLogger<VaultConnector>.Instance)
    {
    }

    public VaultConnector(ILogger logger) : this(logger, _sharedHttpClient, _credentialsCache)
    {
    }

    internal VaultConnector(
        ILogger logger,
        HttpClient httpClient,
        System.Collections.Concurrent.ConcurrentDictionary<long, (Lazy<Task<ClusterProjectCredentialVaultPart>> Task, DateTime ExpiresAtUtc)> cache = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<VaultConnector>.Instance;
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cache = cache ?? _credentialsCache;
    }

    // Static HttpClient prevents Socket Exhaustion issues
    private static readonly HttpClient _sharedHttpClient = new HttpClient {
        BaseAddress = new Uri(VaultConnectorSettings.VaultBaseAddress),
        Timeout = TimeSpan.FromSeconds(VaultConnectorSettings.ConnectionTimeoutInSeconds)
    };

    private void RemoveCacheEntryIfMatches(
        long id,
        (Lazy<Task<ClusterProjectCredentialVaultPart>> Task, DateTime ExpiresAtUtc) entry)
    {
        _cache.TryRemove(
            new KeyValuePair<long, (Lazy<Task<ClusterProjectCredentialVaultPart>> Task, DateTime ExpiresAtUtc)>(id, entry));
    }

    /// <summary>
    /// Get cluster authentication credentials with cache support.
    /// Uses a static cache shared across all instances of the connector.
    /// Empty (failed/not-found) results are never retained in the cache so subsequent requests retry Vault.
    /// </summary>
    public async Task<ClusterProjectCredentialVaultPart> GetClusterAuthenticationCredentials(long id)
    {
        _logger.LogDebug($"Fetching credentials for ID: {id} from Vault.");

        if (_cache.TryGetValue(id, out var entry))
        {
            if (DateTime.UtcNow < entry.ExpiresAtUtc)
            {
                try
                {
                    var cached = await entry.Task.Value;
                    if (cached != ClusterProjectCredentialVaultPart.Empty)
                    {
                        return cached;
                    }

                    RemoveCacheEntryIfMatches(id, entry);
                }
                catch
                {
                    RemoveCacheEntryIfMatches(id, entry);
                    throw;
                }
            }
            else
            {
                RemoveCacheEntryIfMatches(id, entry);
            }
        }

        var expiresAt = DateTime.UtcNow.Add(_credentialsCacheTtl);
        var lazyTask = new Lazy<Task<ClusterProjectCredentialVaultPart>>(() => GetClusterAuthenticationCredentialsInternal(id));
        var newEntry = (Task: lazyTask, ExpiresAtUtc: expiresAt);
        var activeEntry = _cache.AddOrUpdate(
            id,
            _ => newEntry,
            (_, existing) => DateTime.UtcNow < existing.ExpiresAtUtc ? existing : newEntry);

        try
        {
            var result = await activeEntry.Task.Value;
            if (result == ClusterProjectCredentialVaultPart.Empty)
            {
                RemoveCacheEntryIfMatches(id, activeEntry);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to fetch credentials for ID {id} from Vault: {ex.Message}");
            RemoveCacheEntryIfMatches(id, activeEntry);
            throw;
        }
    }

    /// <summary>
    /// Internal method to fetch credentials from Vault.
    /// </summary>
    private async Task<ClusterProjectCredentialVaultPart> GetClusterAuthenticationCredentialsInternal(long id)
    {
        var path = $"{_clusterAuthenticationCredentialsPath}/{id}";
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var result = await _httpClient.GetStringAsync(path);
            sw.Stop();
            var vaultPart = ClusterProjectCredentialVaultPart.FromVaultJsonData(result);
            _logger.LogDebug($"Retrieved vault ClusterProjectCredential with ID: {id}");
            RecordVaultTelemetry("GetClusterAuthenticationCredentials", sw.ElapsedMilliseconds, true, "200", null, path);
            return vaultPart;
        }
        catch (HttpRequestException e)
        {
            sw.Stop();
            _logger.LogWarning($"Vault request for Id: {id} not found. Exception: {e}");
            RecordVaultTelemetry("GetClusterAuthenticationCredentials", sw.ElapsedMilliseconds, false, e.StatusCode?.ToString() ?? "404", e.Message, path);
            return ClusterProjectCredentialVaultPart.Empty;
        }
        catch (Exception ex)
        {
            sw.Stop();
            RecordVaultTelemetry("GetClusterAuthenticationCredentials", sw.ElapsedMilliseconds, false, "500", ex.Message, path);
            throw;
        }
    }

    /// <summary>
    /// Set cluster authentication credentials asynchronously.
    /// Invalidates the shared cache after a successful update.
    /// </summary>
    public async Task<bool> SetClusterAuthenticationCredentialsAsync(ClusterProjectCredentialVaultPart data)
    {
        var path = $"{_clusterAuthenticationCredentialsPath}/{data.Id}";
        var content = data.AsVaultDataJsonObject();
        using var payload = new StringContent(content, Encoding.UTF8, "application/json");

        _logger.LogDebug($"Updating vault ClusterProjectCredential with ID: {data.Id}");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var result = await _httpClient.PostAsync(path, payload);
            sw.Stop();
            var success = result.IsSuccessStatusCode;
            string? errorMessage = null;
            if (!success)
            {
                errorMessage = await result.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to set vault ClusterProjectCredential with ID: {data.Id}. Status: {(int)result.StatusCode} {result.StatusCode}, Response: {errorMessage}");
            }
            RecordVaultTelemetry("SetClusterAuthenticationCredentials", sw.ElapsedMilliseconds, success, ((int)result.StatusCode).ToString(), success ? null : (errorMessage ?? "Non-success status"), path);

            if (success)
            {
                _logger.LogDebug($"Successfully set vault ClusterProjectCredential with ID: {data.Id}");
                if (data != ClusterProjectCredentialVaultPart.Empty)
                {
                    var cachedValue = new Lazy<Task<ClusterProjectCredentialVaultPart>>(() => Task.FromResult(data));
                    _ = cachedValue.Value;
                    _cache[data.Id] = (cachedValue, DateTime.UtcNow.Add(_credentialsCacheTtl));
                }
                else
                {
                    _cache.TryRemove(data.Id, out _);
                }
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            sw.Stop();
            RecordVaultTelemetry("SetClusterAuthenticationCredentials", sw.ElapsedMilliseconds, false, "500", ex.Message, path);
            throw;
        }
    }

    /// <summary>
    /// Delete cluster authentication credentials asynchronously.
    /// Invalidates the shared cache after successful deletion.
    /// </summary>
    public async Task DeleteClusterAuthenticationCredentialsAsync(long id)
    {
        var path = $"{_clusterAuthenticationCredentialsPath}/{id}";
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var result = await _httpClient.DeleteAsync(path);
            sw.Stop();
            var success = result.IsSuccessStatusCode;
            string? errorMessage = null;
            if (!success)
            {
                errorMessage = await result.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to delete vault ClusterProjectCredential with ID: {id}. Status: {(int)result.StatusCode} {result.StatusCode}, Response: {errorMessage}");
            }
            RecordVaultTelemetry("DeleteClusterAuthenticationCredentials", sw.ElapsedMilliseconds, success, ((int)result.StatusCode).ToString(), success ? null : (errorMessage ?? "Non-success status"), path);

            if (success)
            {
                _logger.LogDebug($"Deleted vault ClusterProjectCredential with ID: {id}");
                _cache.TryRemove(id, out _);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            RecordVaultTelemetry("DeleteClusterAuthenticationCredentials", sw.ElapsedMilliseconds, false, "500", ex.Message, path);
            throw;
        }
    }

    private static void RecordVaultTelemetry(string operation, long durationMs, bool isSuccess, string statusCode, string errorMessage, string path, long? clusterId = null)
    {
        try
        {
            HEAppE.Services.Monitoring.ExternalServiceTelemetryService.Instance.Record(new HEAppE.DomainObjects.Monitoring.ExternalServiceHealthLog
            {
                ServiceName = "hashicorp vault",
                ServiceType = "keymanagement",
                Protocol = VaultConnectorSettings.VaultBaseAddress.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? "https" : "http",
                EndpointOrHost = VaultConnectorSettings.VaultBaseAddress,
                CommandOrPath = operation,
                ResponseTimeMs = durationMs,
                IsAvailable = isSuccess,
                StatusCode = statusCode,
                ErrorMessage = errorMessage,
                JobId = HEAppE.Utils.JobExecutionContext.CurrentJobId,
                TaskId = HEAppE.Utils.JobExecutionContext.CurrentTaskId,
                ClusterId = clusterId ?? HEAppE.Utils.JobExecutionContext.CurrentClusterId,
                RequestId = HEAppE.Utils.JobExecutionContext.CurrentRequestId,
                Source = "Execution",
                Timestamp = DateTime.UtcNow
            });
        }
        catch { }
    }
    
    /// <summary>
    /// Create a snapshot of the Vault file-storage backend by zipping the data directory.
    /// </summary>
    /// <returns></returns>

    public async Task<byte[]> CreateSnapshot()
    {
        _logger.LogInformation("Initiating Vault backup using TAR format.");
    
        string vaultSourcePath = "/opt/vault-backup-access/data";

        try
        {
            if (!Directory.Exists(vaultSourcePath))
            {
                _logger.LogError($"Source path {vaultSourcePath} does not exist.");
                return Array.Empty<byte>();
            }

            using (var ms = new MemoryStream())
            {
                await Task.Run(() => TarFile.CreateFromDirectory(vaultSourcePath, ms, false));
                _logger.LogInformation("Vault backup successfully archived into TAR format.");
                return ms.ToArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"TAR backup failed: {ex.Message}");
            return Array.Empty<byte>();
        }
    }

    public async Task<string?> GetClusterSecretAsync(long clusterId, string secretKey)
    {
        var path = $"v1/HEAppE/data/ClusterSecrets/{clusterId}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var responseStr = await _httpClient.GetStringAsync(path);
            sw.Stop();
            RecordVaultTelemetry("GetClusterSecret", sw.ElapsedMilliseconds, true, "200", null, path, clusterId);
            using var doc = JsonDocument.Parse(responseStr);
            if (doc.RootElement.TryGetProperty("data", out var level1) &&
                level1.TryGetProperty("data", out var level2) &&
                level2.TryGetProperty(secretKey, out var valueProp))
            {
                return valueProp.GetString();
            }
        }
        catch (HttpRequestException e)
        {
            sw.Stop();
            _logger.LogWarning($"Vault secret for Cluster {clusterId} key {secretKey} not found: {e.Message}");
            RecordVaultTelemetry("GetClusterSecret", sw.ElapsedMilliseconds, false, e.StatusCode?.ToString() ?? "404", e.Message, path, clusterId);
        }
        catch (Exception ex)
        {
            sw.Stop();
            RecordVaultTelemetry("GetClusterSecret", sw.ElapsedMilliseconds, false, "500", ex.Message, path, clusterId);
            throw;
        }
        return null;
    }

    public async Task<bool> SetClusterSecretAsync(long clusterId, string secretKey, string secretValue)
    {
        var path = $"v1/HEAppE/data/ClusterSecrets/{clusterId}";
        
        // Fetch existing secrets under this cluster path first to prevent overwriting other keys!
        var secrets = new Dictionary<string, string>();
        try
        {
            var responseStr = await _httpClient.GetStringAsync(path);
            using var doc = JsonDocument.Parse(responseStr);
            if (doc.RootElement.TryGetProperty("data", out var level1) &&
                level1.TryGetProperty("data", out var level2))
            {
                foreach (var prop in level2.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        secrets[prop.Name] = prop.Value.GetString() ?? string.Empty;
                    }
                }
            }
        }
        catch (HttpRequestException)
        {
            // Path doesn't exist yet, which is fine
        }

        secrets[secretKey] = secretValue;

        var content = JsonSerializer.Serialize(new { data = secrets });
        using var payload = new StringContent(content, Encoding.UTF8, "application/json");

        _logger.LogDebug($"Updating vault ClusterSecret with ID: {clusterId}");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var result = await _httpClient.PostAsync(path, payload);
            sw.Stop();
            var success = result.IsSuccessStatusCode;
            string? errorMessage = null;
            if (!success)
            {
                errorMessage = await result.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to set vault ClusterSecret for cluster ID: {clusterId}, key: {secretKey}. Status: {(int)result.StatusCode} {result.StatusCode}, Response: {errorMessage}");
            }
            RecordVaultTelemetry("SetClusterSecret", sw.ElapsedMilliseconds, success, ((int)result.StatusCode).ToString(), success ? null : (errorMessage ?? "Non-success status"), path, clusterId);
            return success;
        }
        catch (Exception ex)
        {
            sw.Stop();
            RecordVaultTelemetry("SetClusterSecret", sw.ElapsedMilliseconds, false, "500", ex.Message, path, clusterId);
            throw;
        }
    }

    public async Task DeleteClusterSecretsAsync(long clusterId)
    {
        var path = $"v1/HEAppE/data/ClusterSecrets/{clusterId}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var result = await _httpClient.DeleteAsync(path);
            sw.Stop();
            var success = result.IsSuccessStatusCode;
            string? errorMessage = null;
            if (!success)
            {
                errorMessage = await result.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to delete vault ClusterSecrets with ID: {clusterId}. Status: {(int)result.StatusCode} {result.StatusCode}, Response: {errorMessage}");
            }
            RecordVaultTelemetry("DeleteClusterSecrets", sw.ElapsedMilliseconds, success, ((int)result.StatusCode).ToString(), success ? null : (errorMessage ?? "Non-success status"), path, clusterId);

            if (success)
            {
                _logger.LogDebug($"Deleted vault ClusterSecrets with ID: {clusterId}");
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            RecordVaultTelemetry("DeleteClusterSecrets", sw.ElapsedMilliseconds, false, "500", ex.Message, path, clusterId);
            throw;
        }
    }

    public async Task<Dictionary<string, string>?> GetClusterSecretsAsync(long clusterId)
    {
        var path = $"v1/HEAppE/data/ClusterSecrets/{clusterId}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var responseStr = await _httpClient.GetStringAsync(path);
            sw.Stop();
            RecordVaultTelemetry("GetClusterSecrets", sw.ElapsedMilliseconds, true, "200", null, path, clusterId);
            using var doc = JsonDocument.Parse(responseStr);
            if (doc.RootElement.TryGetProperty("data", out var level1) &&
                level1.TryGetProperty("data", out var level2))
            {
                var secrets = new Dictionary<string, string>();
                foreach (var prop in level2.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        secrets[prop.Name] = prop.Value.GetString() ?? string.Empty;
                    }
                }
                return secrets;
            }
        }
        catch (HttpRequestException e)
        {
            sw.Stop();
            _logger.LogWarning($"Vault secrets for Cluster {clusterId} not found: {e.Message}");
            RecordVaultTelemetry("GetClusterSecrets", sw.ElapsedMilliseconds, false, e.StatusCode?.ToString() ?? "404", e.Message, path, clusterId);
        }
        catch (Exception ex)
        {
            sw.Stop();
            RecordVaultTelemetry("GetClusterSecrets", sw.ElapsedMilliseconds, false, "500", ex.Message, path, clusterId);
            throw;
        }
        return null;
    }

    public async Task<bool> SetClusterSecretsAsync(long clusterId, Dictionary<string, string> secrets)
    {
        var path = $"v1/HEAppE/data/ClusterSecrets/{clusterId}";
        var content = JsonSerializer.Serialize(new { data = secrets });
        using var payload = new StringContent(content, Encoding.UTF8, "application/json");

        _logger.LogDebug($"Updating all vault ClusterSecrets with ID: {clusterId}");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var result = await _httpClient.PostAsync(path, payload);
            sw.Stop();
            var success = result.IsSuccessStatusCode;
            string? errorMessage = null;
            if (!success)
            {
                errorMessage = await result.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to set all vault ClusterSecrets for cluster ID: {clusterId}. Status: {(int)result.StatusCode} {result.StatusCode}, Response: {errorMessage}");
            }
            RecordVaultTelemetry("SetClusterSecrets", sw.ElapsedMilliseconds, success, ((int)result.StatusCode).ToString(), success ? null : (errorMessage ?? "Non-success status"), path, clusterId);
            return success;
        }
        catch (Exception ex)
        {
            sw.Stop();
            RecordVaultTelemetry("SetClusterSecrets", sw.ElapsedMilliseconds, false, "500", ex.Message, path, clusterId);
            throw;
        }
    }
}