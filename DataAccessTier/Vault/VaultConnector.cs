using System;
using System.Net.Http;
using System.Text;

using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.Vault;

internal class VaultConnector : IVaultConnector
{
    public void DeleteClusterAuthenticationCredentials(long id)
    {
        throw new System.NotImplementedException();
    }

    public ClusterProjectCredentialVaultPart GetClusterAuthenticationCredentials(long id)
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:8100");
        var path = @$"v1/HEAppE/data/ClusterAuthenticationCredentials/{id}";
        var resultTask = httpClient.GetStringAsync(path);
        resultTask.Wait(10000);
        var result = resultTask.Result;

        var vaultPart = ClusterProjectCredentialVaultPart.FromVaultJsonData(result);
        return vaultPart;
    }

    public void SetClusterAuthenticationCredentials(ClusterProjectCredentialVaultPart data)
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:8100");
        var path = $@"v1/HEAppE/data/ClusterAuthenticationCredentials/{data.Id}";
        var content = data.AsVaultDataJsonObject();
        var payload = new StringContent(content, Encoding.UTF8, "application/json");
        var messageTask = httpClient.PostAsync(path, payload);
        messageTask.Wait(10000);
        var result = messageTask.Result;
    }
}