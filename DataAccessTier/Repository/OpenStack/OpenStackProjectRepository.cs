using HEAppE.DataAccessTier.IRepository.OpenStack;
using HEAppE.DomainObjects.OpenStack;

namespace HEAppE.DataAccessTier.Repository.OpenStack
{
    internal class OpenStackProjectRepository : GenericRepository<OpenStackProject>, IOpenStackProjectRepository
    {
        #region Constructors
        internal OpenStackProjectRepository(MiddlewareContext context)
            : base(context)
        {
        }
        #endregion
    }
}
