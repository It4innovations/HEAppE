using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.Vault;

internal interface IVaultConnector
{
    ClusterProjectCredentialVaultPart GetClusterAuthenticationCredentials(long id);
    void DeleteClusterAuthenticationCredentials(long id);
    void SetClusterAuthenticationCredentials(ClusterProjectCredentialVaultPart data);
}
