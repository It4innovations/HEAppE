using HEAppE.DomainObjects.ClusterInformation;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface IClusterAuthenticationCredentialsRepository : IRepository<ClusterAuthenticationCredentials>
{
    Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsForClusterAndProject(long clusterId,
        long projectId, bool requireIsInitialized, long? adaptorUserId);

    Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsForUsernameAndProject(
        string username, long projectId, bool requireIsInitialized, long? adaptorUserId);

    Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsProject(long projectId,
        bool requireIsInitialized, long? adaptorUserId);
    Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsProject(string username,
        long projectId, bool requireIsInitialized, long? adaptorUserId);
    Task<ClusterAuthenticationCredentials> GetServiceAccountCredentials(long clusterId, long projectId,
        bool requireIsInitialized, long? adaptorUserId);
    Task<IEnumerable<ClusterAuthenticationCredentials>> GetAllGeneratedWithFingerprint(string fingerprint,
        long projectId);
    Task<IEnumerable<ClusterAuthenticationCredentials>> GetAllGenerated(long projectId);
    
    Task<IList<ClusterAuthenticationCredentials>> GetAllByUserNameAsync(string username);
    
    //GetByIdAsync
    Task<ClusterAuthenticationCredentials> GetByIdAsync(long id);
}