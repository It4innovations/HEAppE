using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using HEAppE.DataAccessTier.Vault.Settings;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.Vault;

public class VaultConnector : IVaultConnector
{
    private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> _idLocks = new();

    private readonly string _vaultBaseAddress = VaultConnectorSettings.VaultBaseAddress;
    private readonly string _clusterPath = VaultConnectorSettings.ClusterAuthenticationCredentialsPath;
    protected readonly ILog _log = LogManager.GetLogger(typeof(VaultConnector));

    public async Task<ClusterProjectCredentialVaultPart> GetClusterAuthenticationCredentials(long id)
    {
        if (_cache.TryGetValue(id, out ClusterProjectCredentialVaultPart cachedData))
            return cachedData;

        var semaphore = _idLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();

        try
        {
            if (_cache.TryGetValue(id, out cachedData)) return cachedData;

            var data = await GetInternalAsync(id);

            // Validation: Only cache if critical data (like PrivateKey or Password) is present
            if (data != null && data.Id != -1)
            {
                _cache.Set(id, data, TimeSpan.FromMinutes(5));
                return data;
            }

            return ClusterProjectCredentialVaultPart.Empty;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<ClusterProjectCredentialVaultPart> GetInternalAsync(long id)
    {
        var requestUrl = $"{_vaultBaseAddress}/{_clusterPath}/{id}";

        try
        {
            // Performance: Use ResponseHeadersRead to minimize memory allocation
            using var response = await _httpClient.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead);
            
            if (!response.IsSuccessStatusCode)
            {
                _log.Warn($"Vault record {id} not found. Status: {response.StatusCode}");
                return null; 
            }

            using var contentStream = await response.Content.ReadAsStreamAsync();
            
            // Deserialize directly from stream
            var envelope = await JsonSerializer.DeserializeAsync<VaultEnvelopeProxy>(contentStream, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return envelope?.Data;
        }
        catch (Exception e)
        {
            _log.Error($"Vault connection error for ID: {id}", e);
            return null;
        }
    }

    // Helper proxy for stream deserialization
    private record VaultEnvelopeProxy([property: JsonPropertyName("data")] ClusterProjectCredentialVaultPart Data);

    public async Task<bool> SetClusterAuthenticationCredentialsAsync(ClusterProjectCredentialVaultPart data)
    {
        var requestUrl = $"{_vaultBaseAddress}/{_clusterPath}/{data.Id}";
        
        // Wrap into expected Vault structure
        var payload = new { data = data };
        using var response = await _httpClient.PostAsJsonAsync(requestUrl, payload);

        if (response.IsSuccessStatusCode)
        {
            _cache.Remove(data.Id); 
            return true;
        }
        return false;
    }

    public async Task DeleteClusterAuthenticationCredentialsAsync(long id)
    {
        var requestUrl = $"{_vaultBaseAddress}/{_clusterPath}/{id}";
        using var response = await _httpClient.DeleteAsync(requestUrl);
        if (response.IsSuccessStatusCode)
        {
            _cache.Remove(id);
            _idLocks.TryRemove(id, out _);
        }
    }
}