using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using HEAppE.DataAccessTier.Vault.Settings;
using HEAppE.DomainObjects.ClusterInformation;


namespace HEAppE.DataAccessTier.Vault;

public class VaultConnector : IVaultConnector
{
    private string _vaultBaseAddress = VaultConnectorSettings.VaultBaseAddress;
    private string _clusterAuthenticationCredentialsPath = VaultConnectorSettings.ClusterAuthenticationCredentialsPath;

    protected readonly ILog _log = LogManager.GetLogger(typeof(VaultConnector));

    public void DeleteClusterAuthenticationCredentials(long id)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_vaultBaseAddress)
        };

        var path = $"{_clusterAuthenticationCredentialsPath}/{id}";
        var messageTask = httpClient.DeleteAsync(path);
        messageTask.Wait(10000);
        var result = messageTask.Result;
        if (result.IsSuccessStatusCode)
        {
            _log.Debug($"Deleted vault ClusterProjectCredential with ID: {id}");
        }
        else
        {
            _log.Warn($"Failed to delete vault ClusterProjectCredential with ID: {id}");
        }
    }

    public async Task<ClusterProjectCredentialVaultPart> GetClusterAuthenticationCredentials(long id)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_vaultBaseAddress)
        };

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


    public bool SetClusterAuthenticationCredentials(ClusterProjectCredentialVaultPart data)
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(_vaultBaseAddress);
        var path = $"{_clusterAuthenticationCredentialsPath}/{data.Id}";
        var content = data.AsVaultDataJsonObject();
        var payload = new StringContent(content, Encoding.UTF8, "application/json");
        _log.Debug($"Updating vault ClusterProjectCredential with ID: {data.Id}");
        var messageTask = httpClient.PostAsync(path, payload);
        messageTask.Wait(10000);
        var result = messageTask.Result;
        if (result.IsSuccessStatusCode)
        {
            _log.Debug($"Set vault ClusterProjectCredential with ID: {data.Id}");
            return true;
        }

        _log.Warn($"Failed to set vault ClusterProjectCredential with ID: {data.Id}");
        return false;
    }

    public async Task<object> GetVaultHealth(int timeoutMs)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_vaultBaseAddress),
            Timeout = TimeSpan.FromMilliseconds(timeoutMs)
        };
        var path = $"/v1/sys/health/";
        
        try
        {
            var result = await httpClient.GetStringAsync(path);
            //var response = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(result);
            var response = JsonConvert.DeserializeObject<ExpandoObject>(result, new ExpandoObjectConverter());
            _log.Warn($"Obtained health information");
            return response;
        }
        catch (Exception e)
        {
            _log.Error($"Vault health check failed. Exception {e}");
        }

        return null;
    }
}