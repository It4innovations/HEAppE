using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.Repository.JobManagement
{
    internal class ClusterProjectRepository : GenericRepository<ClusterProject>, IClusterProjectRepository
    {
        #region Constructors
        internal ClusterProjectRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
        #region Methods
        public ClusterProject GetClusterProjectForJob(long clusterId, long projectId)
        {
            return _context.ClusterProjects.Where(cp => cp.ProjectId == projectId && cp.ClusterId == clusterId).FirstOrDefault();
        }
        #endregion
    }
}
