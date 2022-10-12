using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.JobManagement
{
    public interface IClusterProjectRepository : IRepository<ClusterProject>
    {
        ClusterProject GetClusterProjectForJob(long clusterId, long projectId);
    }
}