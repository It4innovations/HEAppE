using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement
{
    public interface IAdaptorUserRoleRepository : IRepository<AdaptorUserRole>
    {
        /// <summary>
        /// Get the list role.
        /// </summary>
        /// <returns>List user role.</returns>
        AdaptorUserRole GetListRole();

        /// <summary>
        /// Get the read user role.
        /// </summary>
        /// <returns>Read user role.</returns>
        AdaptorUserRole GetReadRole();

        /// <summary>
        /// Get the write user role.
        /// </summary>
        /// <returns>Write user role.</returns>
        AdaptorUserRole GetWriteRole();
    }
}