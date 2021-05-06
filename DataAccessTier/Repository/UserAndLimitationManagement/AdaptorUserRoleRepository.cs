using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement
{
    internal class AdaptorUserRoleRepository : GenericRepository<AdaptorUserRole>, IAdaptorUserRoleRepository
    {
        #region Constructors
        internal AdaptorUserRoleRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
        #region Methods
        public IEnumerable<AdaptorUserRole> GetAllByUserId(long userId)
        {
            return GetAll().SelectMany(s => s.AdaptorUserUserRoles)
                            .Where(w => w.AdaptorUserId == userId)
                            .Select(s => s.AdaptorUserRole)
                            .ToList();
        }

        public IEnumerable<AdaptorUserRole> GetAllByRoleNames(IEnumerable<string> roleNames)
        {
            return GetAll().Where(w => roleNames.Contains(w.Name))
                         .ToList();
        }
        #endregion
    }
}