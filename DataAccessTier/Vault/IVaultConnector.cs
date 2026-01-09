using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;

internal interface IVaultConnector
{
    Task<ClusterProjectCredentialVaultPart> GetClusterAuthenticationCredentials(long id);
    Task DeleteClusterAuthenticationCredentialsAsync(long id);  // async delete
    Task<bool> SetClusterAuthenticationCredentialsAsync(ClusterProjectCredentialVaultPart data);  // async set
}