using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.Vault;

internal interface IVaultConnector
{
    Task<ClusterProjectCredentialVaultPart> GetClusterAuthenticationCredentials(long id);
    void DeleteClusterAuthenticationCredentials(long id);
    bool SetClusterAuthenticationCredentials(ClusterProjectCredentialVaultPart data);
}