using HEAppE.DomainObjects.ClusterInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement
{
    public interface IClusterAuthenticationCredentialsRepository : IRepository<ClusterAuthenticationCredentials>
    {
        IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsForClusterAndProject(long clusterId, long projectId);
        ClusterAuthenticationCredentials GetServiceAccountCredentials(long clusterId, long projectId);
    }
}
