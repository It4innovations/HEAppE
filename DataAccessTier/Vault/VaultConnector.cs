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
    public VaultConnector() : this(Microsoft.Extensions.Logging.Abstractions.NullLogger<VaultConnector>.Instance)
    {
    }

    public VaultConnector(ILogger logger)
    {
        _logger = logger;
    }

    // Static HttpClient prevents Socket Exhaustion issues
    private static readonly HttpClient _httpClient = new HttpClient {
        BaseAddress = new Uri(VaultConnectorSettings.VaultBaseAddress),
        Timeout = TimeSpan.FromSeconds(VaultConnectorSettings.ConnectionTimeoutInSeconds)
    };

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<long, Task<ClusterProjectCredentialVaultPart>> _credentialsCache =
        new System.Collections.Concurrent.ConcurrentDictionary<long, Task<ClusterProjectCredentialVaultPart>>();

    private readonly string _clusterAuthenticationCredentialsPath = VaultConnectorSettings.ClusterAuthenticationCredentialsPath;
    private readonly ILogger _logger;

    /// <summary>
    /// Get cluster authentication credentials with cache support.
    /// Uses a static cache shared across all instances of the connector.
    /// </summary>
    public async Task<ClusterProjectCredentialVaultPart> GetClusterAuthenticationCredentials(long id)
    {
        _logger.LogDebug($"Fetching credentials for ID: {id} from Vault.");
        
        var vaultTask = _credentialsCache.GetOrAdd(id, async key =>
        {
            _logger.LogDebug($"Cache miss. Fetching vault data from service for ID: {key}");
            return await GetClusterAuthenticationCredentialsInternal(key);
        });

        try
        {
            return await vaultTask;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to fetch credentials for ID {id} from Vault: {ex.Message}");
            _credentialsCache.TryRemove(id, out _);
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
        var payload = new StringContent(content, Encoding.UTF8, "application/json");

        _logger.LogDebug($"Updating vault ClusterProjectCredential with ID: {data.Id}");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        HttpResponseMessage result = null;
        try
        {
            result = await _httpClient.PostAsync(path, payload);
            sw.Stop();
            var success = result.IsSuccessStatusCode;
            RecordVaultTelemetry("SetClusterAuthenticationCredentials", sw.ElapsedMilliseconds, success, ((int)result.StatusCode).ToString(), success ? null : "Non-success status", path);

            if (success)
            {
                _logger.LogDebug($"Successfully set vault ClusterProjectCredential with ID: {data.Id}");
                _credentialsCache[data.Id] = Task.FromResult(data);
                return true;
            }

            _logger.LogWarning($"Failed to set vault ClusterProjectCredential with ID: {data.Id}");
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
            var result = await _httpClient.DeleteAsync(path);
            sw.Stop();
            var success = result.IsSuccessStatusCode;
            RecordVaultTelemetry("DeleteClusterAuthenticationCredentials", sw.ElapsedMilliseconds, success, ((int)result.StatusCode).ToString(), success ? null : "Non-success status", path);

            if (success)
            {
                _logger.LogDebug($"Deleted vault ClusterProjectCredential with ID: {id}");
                _credentialsCache.TryRemove(id, out _);
            }
            else
            {
                _logger.LogWarning($"Failed to delete vault ClusterProjectCredential with ID: {id}");
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
        var payload = new StringContent(content, Encoding.UTF8, "application/json");

        _logger.LogDebug($"Updating vault ClusterSecret with ID: {clusterId}");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await _httpClient.PostAsync(path, payload);
            sw.Stop();
            var success = result.IsSuccessStatusCode;
            RecordVaultTelemetry("SetClusterSecret", sw.ElapsedMilliseconds, success, ((int)result.StatusCode).ToString(), success ? null : "Non-success status", path, clusterId);
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
            var result = await _httpClient.DeleteAsync(path);
            sw.Stop();
            var success = result.IsSuccessStatusCode;
            RecordVaultTelemetry("DeleteClusterSecrets", sw.ElapsedMilliseconds, success, ((int)result.StatusCode).ToString(), success ? null : "Non-success status", path, clusterId);

            if (success)
            {
                _logger.LogDebug($"Deleted vault ClusterSecrets with ID: {clusterId}");
            }
            else
            {
                _logger.LogWarning($"Failed to delete vault ClusterSecrets with ID: {clusterId}");
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
        var payload = new StringContent(content, Encoding.UTF8, "application/json");

        _logger.LogDebug($"Updating all vault ClusterSecrets with ID: {clusterId}");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await _httpClient.PostAsync(path, payload);
            sw.Stop();
            var success = result.IsSuccessStatusCode;
            RecordVaultTelemetry("SetClusterSecrets", sw.ElapsedMilliseconds, success, ((int)result.StatusCode).ToString(), success ? null : "Non-success status", path, clusterId);
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