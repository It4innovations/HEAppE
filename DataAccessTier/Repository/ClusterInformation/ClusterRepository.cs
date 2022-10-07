using HEAppE.DataAccessTier.IRepository.ClusterInformation;
using HEAppE.DomainObjects.ClusterInformation;
using System.Linq;

namespace HEAppE.DataAccessTier.Repository.ClusterInformation
{
    internal class ClusterRepository : GenericRepository<Cluster>, IClusterRepository
    {
        #region Constructors
        internal ClusterRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion

        #region Methods
        public Cluster GetClusterForProject(long clusterId, long projectId)
        {
            var clusterProject = _context.ClusterProjects.FirstOrDefault(p => p.ClusterId == clusterId && p.ProjectId == projectId);
            return clusterProject.Cluster;
        }
        #endregion
    }
}