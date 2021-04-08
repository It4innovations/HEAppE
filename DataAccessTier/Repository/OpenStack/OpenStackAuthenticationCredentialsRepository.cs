using System.Linq;
using HEAppE.DataAccessTier.IRepository.OpenStack;
using HEAppE.DomainObjects.OpenStack;

namespace HEAppE.DataAccessTier.Repository.OpenStack
{
    internal class OpenStackAuthenticationCredentialsRepository : GenericRepository<OpenStackAuthenticationCredentials>,
        IOpenStackAuthenticationCredentialsRepository
    {
        #region Constructors
        internal OpenStackAuthenticationCredentialsRepository(MiddlewareContext context)
            : base(context)
        {
        }
        #endregion
        #region Methods
        public OpenStackAuthenticationCredentials GetDefaultAccount()
        {
            return GetAll().Single();
        }
        #endregion
    }
}