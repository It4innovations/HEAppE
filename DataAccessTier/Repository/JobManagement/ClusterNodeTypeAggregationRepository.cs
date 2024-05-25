using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.JobManagement
{
    internal class ClusterNodeTypeAggregationRepository : GenericRepository<ClusterNodeTypeAggregation>, IClusterNodeTypeAggregationRepository
    {
        #region Constructors
        internal ClusterNodeTypeAggregationRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}
