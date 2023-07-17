using HEAppE.DataAccessTier.IRepository.ClusterInformation;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.Repository.ClusterInformation
{
    internal class ClusterNodeTypeRequestedGroupRepository : GenericRepository<ClusterNodeTypeRequestedGroup>, IClusterNodeTypeRequestedGroupRepository
    {
        #region Constructors
        internal ClusterNodeTypeRequestedGroupRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}
