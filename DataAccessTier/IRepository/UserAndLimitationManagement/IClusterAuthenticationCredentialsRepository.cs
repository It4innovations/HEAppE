using System.Collections.Generic;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface IClusterAuthenticationCredentialsRepository : IRepository<ClusterAuthenticationCredentials>
{
    IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsForClusterAndProject(long clusterId,
        long projectId);

    IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsForUsernameAndProject(string username,
        long projectId);

    IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsProject(long projectId);
    ClusterAuthenticationCredentials GetServiceAccountCredentials(long clusterId, long projectId);
    IEnumerable<ClusterAuthenticationCredentials> GetAllGeneratedWithFingerprint(string fingerprint, long projectId);
    IEnumerable<ClusterAuthenticationCredentials> GetAllGenerated(long projectId);
}