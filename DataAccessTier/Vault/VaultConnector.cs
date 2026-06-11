using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
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

        try
        {
            var result = await _httpClient.GetStringAsync(path);
            var vaultPart = ClusterProjectCredentialVaultPart.FromVaultJsonData(result);
            _logger.LogDebug($"Retrieved vault ClusterProjectCredential with ID: {id}");
            return vaultPart;
        }
        catch (HttpRequestException e)
        {
            _logger.LogWarning($"Vault request for Id: {id} not found. Exception: {e}");
            return ClusterProjectCredentialVaultPart.Empty;
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
        var result = await _httpClient.PostAsync(path, payload);

        if (result.IsSuccessStatusCode)
        {
            _logger.LogDebug($"Successfully set vault ClusterProjectCredential with ID: {data.Id}");
            _credentialsCache[data.Id] = Task.FromResult(data);
            return true;
        }

        _logger.LogWarning($"Failed to set vault ClusterProjectCredential with ID: {data.Id}");
        return false;
    }

    /// <summary>
    /// Delete cluster authentication credentials asynchronously.
    /// Invalidates the shared cache after successful deletion.
    /// </summary>
    public async Task DeleteClusterAuthenticationCredentialsAsync(long id)
    {
        var path = $"{_clusterAuthenticationCredentialsPath}/{id}";

        var result = await _httpClient.DeleteAsync(path);

        if (result.IsSuccessStatusCode)
        {
            _logger.LogDebug($"Deleted vault ClusterProjectCredential with ID: {id}");
            _credentialsCache.TryRemove(id, out _);
        }
        else
        {
            _logger.LogWarning($"Failed to delete vault ClusterProjectCredential with ID: {id}");
        }
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
}