using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System.Linq;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement
{
    internal class AdaptorUserRoleRepository : GenericRepository<AdaptorUserRole>, IAdaptorUserRoleRepository
    {
        #region Constants
        private const string ListRoleName = "List";
        private const string ReadRoleName = "Read";
        private const string WriteRoleName = "Write";
        #endregion

        // These three roles should never change and can be cached without any validation.
        #region CachedRoles
        /// <summary>
        /// List user role.
        /// </summary>
        private AdaptorUserRole _listRole;

        /// <summary>
        /// Read user role.
        /// </summary>
        private AdaptorUserRole _readRole;

        /// <summary>
        /// Write user role.
        /// </summary>
        private AdaptorUserRole _writeRole;
        #endregion

        #region Constructors
        internal AdaptorUserRoleRepository(MiddlewareContext context)
            : base(context)
        {

        }

        /// <inheritdoc/>
        public AdaptorUserRole GetListRole()
        {
            if (_listRole is null)
            {
                _listRole = _context.AdaptorUserRoles.Single(role => role.Name == ListRoleName);
            }
            return _listRole;
        }

        /// <inheritdoc/>
        public AdaptorUserRole GetReadRole()
        {
            if (_readRole is null)
            {
                _readRole = _context.AdaptorUserRoles.Single(role => role.Name == ReadRoleName);
            }
            return _readRole;
        }

        /// <inheritdoc/>
        public AdaptorUserRole GetWriteRole()
        {
            if (_writeRole is null)
            {
                _writeRole = _context.AdaptorUserRoles.Single(role => role.Name == WriteRoleName);
            }
            return _writeRole;
        }
        #endregion
    }
}