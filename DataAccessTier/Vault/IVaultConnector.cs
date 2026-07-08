using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;

internal interface IVaultConnector
{
    Task<ClusterProjectCredentialVaultPart> GetClusterAuthenticationCredentials(long id);
    Task DeleteClusterAuthenticationCredentialsAsync(long id);  // async delete
    Task<bool> SetClusterAuthenticationCredentialsAsync(ClusterProjectCredentialVaultPart data);  // async set
    Task<byte[]> CreateSnapshot();

    // Generic Cluster Secret Storage Methods
    Task<string?> GetClusterSecretAsync(long clusterId, string secretKey);
    Task<bool> SetClusterSecretAsync(long clusterId, string secretKey, string secretValue);
    Task DeleteClusterSecretsAsync(long clusterId);
    Task<Dictionary<string, string>?> GetClusterSecretsAsync(long clusterId);
    Task<bool> SetClusterSecretsAsync(long clusterId, Dictionary<string, string> secrets);
}