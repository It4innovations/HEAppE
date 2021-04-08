using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement
{
    internal class ResourceLimitationRepository : GenericRepository<ResourceLimitation>, IResourceLimitationRepository
    {
        #region Constructors
        internal ResourceLimitationRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}