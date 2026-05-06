using HEAppE.DataAccessTier.IRepository.FirecRest;
using HEAppE.DomainObjects.FirecRest;

namespace HEAppE.DataAccessTier.Repository.FirecRest;

internal class FirecRestEndpointRepository : GenericRepository<FirecRestEndpoint>, IFirecRestEndpointRepository
{
    #region Constructors

    internal FirecRestEndpointRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion
}