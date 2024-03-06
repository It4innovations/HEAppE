using System;
using System.Net.Http;
using System.Text;

using HEAppE.DomainObjects.ClusterInformation;

using log4net;

namespace HEAppE.DataAccessTier.Vault;

internal class VaultConnector : IVaultConnector
{
    protected readonly ILog _log = LogManager.GetLogger(typeof(VaultConnector));
    public void DeleteClusterAuthenticationCredentials(long id)
    {
        throw new System.NotImplementedException();
    }

    public ClusterProjectCredentialVaultPart GetClusterAuthenticationCredentials(long id)
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://vaultagent:8100");
        var path = @$"v1/HEAppE/data/ClusterAuthenticationCredentials/{id}";
        try
        {
            var resultTask = httpClient.GetStringAsync(path);
            resultTask.Wait(10000);
            var result = resultTask.Result;

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
        httpClient.BaseAddress = new Uri("http://vaultagent:8100");
        var path = $@"v1/HEAppE/data/ClusterAuthenticationCredentials/{data.Id}";
        var content = data.AsVaultDataJsonObject();
        var payload = new StringContent(content, Encoding.UTF8, "application/json");
        var messageTask = httpClient.PostAsync(path, payload);
        messageTask.Wait(10000);
        var result = messageTask.Result;
    }
}