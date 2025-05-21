using System.Collections.Generic;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface IClusterAuthenticationCredentialsRepository : IRepository<ClusterAuthenticationCredentials>
{
    IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsForClusterAndProject(long clusterId,
        long projectId, long? adaptorUserId);

    IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsForUsernameAndProject(string username,
        long projectId, long? adaptorUserId);

    IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsProject(long projectId, long? adaptorUserId);
    ClusterAuthenticationCredentials GetServiceAccountCredentials(long clusterId, long projectId, long? adaptorUserId);
    IEnumerable<ClusterAuthenticationCredentials> GetAllGeneratedWithFingerprint(string fingerprint, long projectId);
    IEnumerable<ClusterAuthenticationCredentials> GetAllGenerated(long projectId);
}