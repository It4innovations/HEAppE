using HEAppE.DomainObjects.ClusterInformation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface IClusterAuthenticationCredentialsRepository : IRepository<ClusterAuthenticationCredentials>
{
    IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsForClusterAndProject(long clusterId,
        long projectId, bool requireIsInitialized, long? adaptorUserId);

    IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsForUsernameAndProject(string username,
        long projectId, bool requireIsInitialized, long? adaptorUserId);

    IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsProject(long projectId, bool requireIsInitialized, long? adaptorUserId);
    ClusterAuthenticationCredentials GetServiceAccountCredentials(long clusterId, long projectId, bool requireIsInitialized, long? adaptorUserId);
    IEnumerable<ClusterAuthenticationCredentials> GetAllGeneratedWithFingerprint(string fingerprint, long projectId);
    IEnumerable<ClusterAuthenticationCredentials> GetAllGenerated(long projectId);

    Task<object> GetVaultHealth();
}