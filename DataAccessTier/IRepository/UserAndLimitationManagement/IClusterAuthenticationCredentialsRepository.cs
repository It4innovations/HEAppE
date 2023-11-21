﻿using HEAppE.DomainObjects.ClusterInformation;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement
{
    public interface IClusterAuthenticationCredentialsRepository : IRepository<ClusterAuthenticationCredentials>
    {
        IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsForClusterAndProject(long clusterId, long projectId);
        ClusterAuthenticationCredentials GetServiceAccountCredentials(long clusterId, long projectId);
        IEnumerable<ClusterAuthenticationCredentials> GetAllGeneratedWithFingerprint(string fingerprint, long projectId);
    }
}
