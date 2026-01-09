using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using HEAppE.DataAccessTier.Vault.Settings;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.Vault;

public class VaultConnector : IVaultConnector
{
    // Static cache ensures data is shared across all instances of VaultConnector
    private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    
    // Static HttpClient prevents Socket Exhaustion (prevents staying in TIME_WAIT state)
    private static readonly HttpClient _httpClient = new HttpClient();
    
    // Semaphore to prevent "Cache Stampede" (multiple threads calling Vault for the same ID simultaneously)
    private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    private readonly string _vaultBaseAddress = VaultConnectorSettings.VaultBaseAddress;
    private readonly string _clusterAuthenticationCredentialsPath = VaultConnectorSettings.ClusterAuthenticationCredentialsPath;
    protected readonly ILog _log = LogManager.GetLogger(typeof(VaultConnector));

    /// <summary>
    /// Fetches cluster credentials with thread-safe caching.
    /// </summary>
    public async Task<ClusterProjectCredentialVaultPart> GetClusterAuthenticationCredentials(long id)
    {
        // 1. Fast path: Check if the item exists in cache without locking
        if (_cache.TryGetValue(id, out ClusterProjectCredentialVaultPart cachedData))
        {
            return cachedData;
        }

        // 2. Slow path: Synchronize threads to fetch data from Vault
        await _lock.WaitAsync();
        try
        {
            // Double-check pattern: ensure another thread didn't fill the cache while we were waiting for the lock
            return await _cache.GetOrCreateAsync(id, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(5);
                _log.Debug($"Cache miss for ID: {id}. Fetching from Vault.");
                return GetClusterAuthenticationCredentialsInternal(id);
            });
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Internal logic to perform the actual HTTP GET request to Vault.
    /// </summary>
    private async Task<ClusterProjectCredentialVaultPart> GetClusterAuthenticationCredentialsInternal(long id)
    {
        var requestUrl = $"{_vaultBaseAddress}/{_clusterAuthenticationCredentialsPath}/{id}";

        try
        {
            var response = await _httpClient.GetStringAsync(requestUrl);
            var vaultPart = ClusterProjectCredentialVaultPart.FromVaultJsonData(response);
            _log.Debug($"Successfully retrieved vault credentials for ID: {id}");
            return vaultPart;
        }
        catch (HttpRequestException e)
        {
            _log.Warn($"Vault request for Id: {id} failed or not found. Exception: {e.Message}");
            return ClusterProjectCredentialVaultPart.Empty;
        }
    }

    /// <summary>
    /// Updates credentials in Vault and invalidates the local cache.
    /// </summary>
    public async Task<bool> SetClusterAuthenticationCredentialsAsync(ClusterProjectCredentialVaultPart data)
    {
        var requestUrl = $"{_vaultBaseAddress}/{_clusterAuthenticationCredentialsPath}/{data.Id}";
        var jsonContent = data.AsVaultDataJsonObject();
        var payload = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        _log.Debug($"Updating vault credentials for ID: {data.Id}");
        
        var response = await _httpClient.PostAsync(requestUrl, payload);

        if (response.IsSuccessStatusCode)
        {
            _log.Debug($"Vault update successful for ID: {data.Id}. Invalidating cache.");
            _cache.Remove(data.Id); 
            return true;
        }

        _log.Warn($"Failed to update Vault for ID: {data.Id}. Status Code: {response.StatusCode}");
        return false;
    }

    /// <summary>
    /// Deletes credentials from Vault and removes them from the local cache.
    /// </summary>
    public async Task DeleteClusterAuthenticationCredentialsAsync(long id)
    {
        var requestUrl = $"{_vaultBaseAddress}/{_clusterAuthenticationCredentialsPath}/{id}";

        var response = await _httpClient.DeleteAsync(requestUrl);

        if (response.IsSuccessStatusCode)
        {
            _log.Debug($"Deleted vault credentials for ID: {id}. Removing from cache.");
            _cache.Remove(id);
        }
        else
        {
            _log.Warn($"Failed to delete vault credentials for ID: {id}. Status Code: {response.StatusCode}");
        }
    }
}