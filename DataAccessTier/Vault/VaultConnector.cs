using System;
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
    // In-memory cache to store retrieved credentials for parallel-safe access
    private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private string _vaultBaseAddress = VaultConnectorSettings.VaultBaseAddress;
    private string _clusterAuthenticationCredentialsPath = VaultConnectorSettings.ClusterAuthenticationCredentialsPath;
    protected readonly ILog _log = LogManager.GetLogger(typeof(VaultConnector));

    /// <summary>
    /// Get cluster authentication credentials with cache support.
    /// Parallel calls for the same ID will wait for a single HTTP request.
    /// </summary>
    public async Task<ClusterProjectCredentialVaultPart> GetClusterAuthenticationCredentials(long id)
    {
        // Use cache with sliding expiration
        return await _cache.GetOrCreateAsync(id, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(5);
            return GetClusterAuthenticationCredentialsInternal(id);
        });
    }

    /// <summary>
    /// Internal method to fetch credentials from Vault.
    /// </summary>
    private async Task<ClusterProjectCredentialVaultPart> GetClusterAuthenticationCredentialsInternal(long id)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(_vaultBaseAddress) };
        var path = $"{_clusterAuthenticationCredentialsPath}/{id}";

        try
        {
            var result = await httpClient.GetStringAsync(path);
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
    /// Invalidates cache after a successful update.
    /// </summary>
    public async Task<bool> SetClusterAuthenticationCredentialsAsync(ClusterProjectCredentialVaultPart data)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(_vaultBaseAddress) };
        var path = $"{_clusterAuthenticationCredentialsPath}/{data.Id}";
        var content = data.AsVaultDataJsonObject();
        var payload = new StringContent(content, Encoding.UTF8, "application/json");

        _log.Debug($"Updating vault ClusterProjectCredential with ID: {data.Id}");
        var result = await httpClient.PostAsync(path, payload);

        if (result.IsSuccessStatusCode)
        {
            _log.Debug($"Set vault ClusterProjectCredential with ID: {data.Id}");
            _cache.Remove(data.Id); // Invalidate cache
            return true;
        }

        _log.Warn($"Failed to set vault ClusterProjectCredential with ID: {data.Id}");
        return false;
    }

    /// <summary>
    /// Delete cluster authentication credentials asynchronously.
    /// Invalidates cache after successful deletion.
    /// </summary>
    public async Task DeleteClusterAuthenticationCredentialsAsync(long id)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(_vaultBaseAddress) };
        var path = $"{_clusterAuthenticationCredentialsPath}/{id}";

        var result = await httpClient.DeleteAsync(path);

        if (result.IsSuccessStatusCode)
        {
            _log.Debug($"Deleted vault ClusterProjectCredential with ID: {id}");
            _cache.Remove(id); // Invalidate cache
        }
        else
        {
            _log.Warn($"Failed to delete vault ClusterProjectCredential with ID: {id}");
        }
    }
}
