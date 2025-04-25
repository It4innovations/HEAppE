using HEAppE.DataAccessTier.IRepository.OpenStack;
using HEAppE.DomainObjects.OpenStack;

namespace HEAppE.DataAccessTier.Repository.OpenStack;

internal class OpenStackDomainRepository : GenericRepository<OpenStackDomain>, IOpenStackDomainRepository
{
    #region Constructors

    internal OpenStackDomainRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion
}