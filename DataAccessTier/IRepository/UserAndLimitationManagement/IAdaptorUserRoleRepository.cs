using System.Collections.Generic;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface IAdaptorUserRoleRepository : IRepository<AdaptorUserRole>
{
    AdaptorUserRole GetByRoleName(string roleName);
    AdaptorUserRole GetByRoleNames(IEnumerable<string> roleNames);
}