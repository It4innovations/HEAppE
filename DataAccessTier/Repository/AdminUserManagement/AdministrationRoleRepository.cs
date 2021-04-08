using HEAppE.DataAccessTier.IRepository.AdminUserManagement;
using HEAppE.DomainObjects.AdminUserManagement;

namespace HEAppE.DataAccessTier.Repository.AdminUserManagement
{
    internal class AdministrationRoleRepository : GenericRepository<AdministrationRole>, IAdministrationRoleRepository
    {
        #region Constructors
        internal AdministrationRoleRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}