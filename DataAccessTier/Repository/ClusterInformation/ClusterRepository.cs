using HEAppE.DataAccessTier.IRepository.ClusterInformation;
using HEAppE.DomainObjects.ClusterInformation;

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
    }
}