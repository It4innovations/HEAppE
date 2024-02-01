using HEAppE.DomainObjects.UserAndLimitationManagement;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement
{
    public interface IAdaptorUserRoleRepository : IRepository<AdaptorUserRole>
    {
        AdaptorUserRole GetByRoleName(string roleName);
        AdaptorUserRole GetByRoleNames(IEnumerable<string> roleNames);
    }
}