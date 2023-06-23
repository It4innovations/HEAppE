using HEAppE.DataAccessTier.IRepository.OpenStack;
using HEAppE.DomainObjects.OpenStack;

namespace HEAppE.DataAccessTier.Repository.OpenStack
{
    internal class OpenStackAuthenticationCredentialsRepository : GenericRepository<OpenStackAuthenticationCredential>,
        IOpenStackAuthenticationCredentialsRepository
    {
        #region Constructors
        internal OpenStackAuthenticationCredentialsRepository(MiddlewareContext context)
            : base(context)
        {
        }
        #endregion
    }
}