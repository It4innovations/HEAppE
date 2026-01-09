using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using HEAppE.DataAccessTier.Vault.Settings;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.Vault;

public class VaultConnector : IVaultConnector
{
    // Static cache shared across instances to maximize hit rate
    private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    
    // Reusing HttpClient to prevent Socket Exhaustion
    private static readonly HttpClient _httpClient = new HttpClient();
    
    // Granular locks to allow concurrent fetching of different IDs
    // Performance: This prevents a global bottleneck
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> _idLocks = new();

    private readonly string _vaultBaseAddress = VaultConnectorSettings.VaultBaseAddress;
    private readonly string _clusterPath = VaultConnectorSettings.ClusterAuthenticationCredentialsPath;
    protected readonly ILog _log = LogManager.GetLogger(typeof(VaultConnector));

    /// <summary>
    /// Fetches cluster credentials with high-performance thread-safe caching.
    /// </summary>
    public async Task<ClusterProjectCredentialVaultPart> GetClusterAuthenticationCredentials(long id)
    {
        // L1 Path: Fast, lock-free cache lookup
        if (_cache.TryGetValue(id, out ClusterProjectCredentialVaultPart cachedData))
        {
            return cachedData;
        }

        // L2 Path: Synchronized fetch. Only block threads requesting the SAME ID.
        var semaphore = _idLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();

        try
        {
            // Double-check pattern to handle race conditions during lock acquisition
            if (_cache.TryGetValue(id, out cachedData))
            {
                return cachedData;
            }

            _log.Debug($"Cache miss for ID: {id}. Fetching from Vault.");
            var data = await GetInternalAsync(id);

            // Cache the result for 5 minutes
            _cache.Set(id, data, new MemoryCacheEntryOptions 
            { 
                SlidingExpiration = TimeSpan.FromMinutes(5) 
            });

            return data;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Performs an optimized HTTP GET using streams to minimize memory allocations.
    /// </summary>
    private async Task<ClusterProjectCredentialVaultPart> GetInternalAsync(long id)
    {
        var requestUrl = $"{_vaultBaseAddress}/{_clusterPath}/{id}";

        try
        {
            // Performance: ResponseHeadersRead prevents the client from buffering the whole body into a string
            using var response = await _httpClient.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead);
            
            if (!response.IsSuccessStatusCode)
            {
                _log.Warn($"Vault returned {response.StatusCode} for ID: {id}");
                return ClusterProjectCredentialVaultPart.Empty;
            }

            // Performance: Deserialize directly from the stream to avoid Large Object Heap (LOH) fragmentation
            using var contentStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<ClusterProjectCredentialVaultPart>(contentStream) 
                   ?? ClusterProjectCredentialVaultPart.Empty;
        }
        catch (Exception e)
        {
            _log.Error($"Critical failure retrieving Vault ID: {id}", e);
            return ClusterProjectCredentialVaultPart.Empty;
        }
    }

    /// <summary>
    /// Updates credentials using high-performance JSON streaming and invalidates cache.
    /// </summary>
    public async Task<bool> SetClusterAuthenticationCredentialsAsync(ClusterProjectCredentialVaultPart data)
    {
        var requestUrl = $"{_vaultBaseAddress}/{_clusterPath}/{data.Id}";

        // Performance: PostAsJsonAsync serializes directly to the request stream
        using var response = await _httpClient.PostAsJsonAsync(requestUrl, data);

        if (response.IsSuccessStatusCode)
        {
            _log.Debug($"Vault update successful for ID: {data.Id}. Invalidating cache.");
            _cache.Remove(data.Id); 
            return true;
        }

        _log.Warn($"Failed to update Vault for ID: {data.Id}. Status: {response.StatusCode}");
        return false;
    }

    /// <summary>
    /// Deletes credentials and ensures cache consistency.
    /// </summary>
    public async Task DeleteClusterAuthenticationCredentialsAsync(long id)
    {
        var requestUrl = $"{_vaultBaseAddress}/{_clusterPath}/{id}";

        using var response = await _httpClient.DeleteAsync(requestUrl);

        if (response.IsSuccessStatusCode)
        {
            _cache.Remove(id);
            _idLocks.TryRemove(id, out _); // Cleanup the lock object to save memory
        }
    }
}