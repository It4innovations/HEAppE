using HEAppE.DataAccessTier.IRepository.ClusterInformation;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.Repository.ClusterInformation
{
    internal class ClusterProxyConnectionRepository : GenericRepository<ClusterProxyConnection>, IClusterProxyConnectionRepository
    {
        #region Constructors
        internal ClusterProxyConnectionRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}
