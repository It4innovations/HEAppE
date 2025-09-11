using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.JobManagement;

internal class ClusterProjectRepository : GenericRepository<ClusterProject>, IClusterProjectRepository
{
    #region Constructors

    internal ClusterProjectRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public ClusterProject GetClusterProjectForClusterAndProject(long clusterId, long projectId)
    {
        return _context.ClusterProjects.Where(cp => cp.ProjectId == projectId && cp.ClusterId == clusterId)
            .FirstOrDefault();
    }
    
    public List<ClusterProject> GetClusterProjectForCluster(long projectId)
    {
        return _context.ClusterProjects.Where(cp => cp.ProjectId == projectId)
            .ToList();
    }

    #endregion
}