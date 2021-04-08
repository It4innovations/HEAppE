using HEAppE.DataAccessTier.IRepository.AdminUserManagement;
using HEAppE.DomainObjects.AdminUserManagement;

namespace HEAppE.DataAccessTier.Repository.AdminUserManagement
{
    internal class AdministrationUserRepository : GenericRepository<AdministrationUser>, IAdministrationUserRepository
    {
        #region Constructors
        internal AdministrationUserRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}