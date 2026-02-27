using HEAppE.DomainObjects.ClusterInformation;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface IClusterAuthenticationCredentialsRepository : IRepository<ClusterAuthenticationCredentials>
{
    Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsForClusterAndProject(long clusterId,
        long projectId, bool requireIsInitialized, long? adaptorUserId, ILogger logger);

    Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsForUsernameAndProject(
        string username, long projectId, bool requireIsInitialized, long? adaptorUserId, ILogger logger);

    Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsProject(long projectId,
        bool requireIsInitialized, long? adaptorUserId, ILogger logger, bool isAdministrator = false);
    Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsProject(string username,
        long projectId, bool requireIsInitialized, long? adaptorUserId, ILogger logger, bool isAdministrator = false);
    Task<ClusterAuthenticationCredentials> GetServiceAccountCredentials(long clusterId, long projectId,
        bool requireIsInitialized, long? adaptorUserId, ILogger logger);
    Task<IEnumerable<ClusterAuthenticationCredentials>> GetAllGeneratedWithFingerprint(string fingerprint,
        long projectId, ILogger logger);
    Task<IEnumerable<ClusterAuthenticationCredentials>> GetAllGenerated(long projectId, ILogger logger);
    
    Task<IList<ClusterAuthenticationCredentials>> GetAllByUserNameAsync(string username, ILogger logger);
    
    //GetByIdAsync
    Task<ClusterAuthenticationCredentials> GetByIdAsync(long id);
}