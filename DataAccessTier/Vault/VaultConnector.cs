using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;

using log4net;

namespace HEAppE.DataAccessTier.Vault;

internal class VaultConnector : IVaultConnector
{
    private const string _vaultBaseAddress = "http://vaultagent:8100";
    private const string _clusterAuthenticationCredentialsPath = "v1/HEAppE/data/ClusterAuthenticationCredentials";

    protected readonly ILog _log = LogManager.GetLogger(typeof(VaultConnector));

    public void DeleteClusterAuthenticationCredentials(long id)
    {
        throw new System.NotImplementedException();
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
            return vaultPart;
        }
        catch (HttpRequestException e)
        {
            _log.Warn($"Vault request for Id: {id} not found. Exception: {e}");
            return ClusterProjectCredentialVaultPart.Empty;
        }
    }


    public void SetClusterAuthenticationCredentials(ClusterProjectCredentialVaultPart data)
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(_vaultBaseAddress);
        var path = $"{_clusterAuthenticationCredentialsPath}/{data.Id}";
        var content = data.AsVaultDataJsonObject();
        var payload = new StringContent(content, Encoding.UTF8, "application/json");
        var messageTask = httpClient.PostAsync(path, payload);
        messageTask.Wait(10000);
        var result = messageTask.Result;
        _log.Debug($"Update vault ClusterProjectCredential with ID: {data.Id}");
    }
}
