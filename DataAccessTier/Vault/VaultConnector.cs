using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using HEAppE.DataAccessTier.Vault.Settings;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.Vault;

public class VaultConnector : IVaultConnector
{
    // Static members ensure the cache and client are shared across all instances of VaultConnector
    private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    
    // Static HttpClient prevents Socket Exhaustion issues
    private static readonly HttpClient _httpClient = new HttpClient { 
        BaseAddress = new Uri(VaultConnectorSettings.VaultBaseAddress) 
    };

    private readonly string _clusterAuthenticationCredentialsPath = VaultConnectorSettings.ClusterAuthenticationCredentialsPath;
    protected readonly ILog _log = LogManager.GetLogger(typeof(VaultConnector));

    /// <summary>
    /// Get cluster authentication credentials with cache support.
    /// Uses a static cache shared across all instances of the connector.
    /// </summary>
    public async Task<ClusterProjectCredentialVaultPart> GetClusterAuthenticationCredentials(long id)
    {
        // GetOrCreateAsync handles locking, so only one HTTP request is made per ID
        return await _cache.GetOrCreateAsync(id, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(5);
            _log.Debug($"Cache miss for ID: {id}, fetching from Vault.");
            return GetClusterAuthenticationCredentialsInternal(id);
        });
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
            _log.Debug($"Retrieved vault ClusterProjectCredential with ID: {id}");
            return vaultPart;
        }
        catch (HttpRequestException e)
        {
            _log.Warn($"Vault request for Id: {id} not found. Exception: {e}");
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

        _log.Debug($"Updating vault ClusterProjectCredential with ID: {data.Id}");
        var result = await _httpClient.PostAsync(path, payload);

        if (result.IsSuccessStatusCode)
        {
            _log.Debug($"Successfully set vault ClusterProjectCredential with ID: {data.Id}");
            _cache.Remove(data.Id); // Invalidate shared cache
            return true;
        }

        _log.Warn($"Failed to set vault ClusterProjectCredential with ID: {data.Id}");
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
            _log.Debug($"Deleted vault ClusterProjectCredential with ID: {id}");
            _cache.Remove(id); // Invalidate shared cache
        }
        else
        {
            _log.Warn($"Failed to delete vault ClusterProjectCredential with ID: {id}");
        }
    }
    
    /// <summary>
    /// Create a snapshot of the Vault file-storage backend by zipping the data directory.
    /// </summary>
    /// <returns></returns>

    public async Task<byte[]> CreateSnapshot()
    {
        _log.Info("Initiating Vault backup using TAR format.");
    
        string vaultSourcePath = "/opt/vault-backup-access/data";

        try
        {
            if (!Directory.Exists(vaultSourcePath))
            {
                _log.Error($"Source path {vaultSourcePath} does not exist.");
                return Array.Empty<byte>();
            }

            using (var ms = new MemoryStream())
            {
                await Task.Run(() => TarFile.CreateFromDirectory(vaultSourcePath, ms, false));
                _log.Info("Vault backup successfully archived into TAR format.");
                return ms.ToArray();
            }
        }
        catch (Exception ex)
        {
            _log.Error($"TAR backup failed: {ex.Message}");
            return Array.Empty<byte>();
        }
    }
}