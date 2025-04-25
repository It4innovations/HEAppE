using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement;

public interface IClusterProjectRepository : IRepository<ClusterProject>
{
    ClusterProject GetClusterProjectForClusterAndProject(long clusterId, long projectId);
}