using HEAppE.DataAccessTier.IRepository.OpenStack;
using HEAppE.DomainObjects.OpenStack;

namespace HEAppE.DataAccessTier.Repository.OpenStack
{
    internal class OpenStackProjectDomainRepository : GenericRepository<OpenStackProjectDomain>, IOpenStackProjectDomainRepository
    {
        #region Constructors
        internal OpenStackProjectDomainRepository(MiddlewareContext context)
            : base(context)
        {
        }
        #endregion
    }
}
