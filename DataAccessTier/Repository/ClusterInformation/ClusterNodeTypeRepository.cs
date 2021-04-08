using HEAppE.DataAccessTier.IRepository.ClusterInformation;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.Repository.ClusterInformation
{
    internal class ClusterNodeTypeRepository : GenericRepository<ClusterNodeType>, IClusterNodeTypeRepository
    {
        #region Constructors
        internal ClusterNodeTypeRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}