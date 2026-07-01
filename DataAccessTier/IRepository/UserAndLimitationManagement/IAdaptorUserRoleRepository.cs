using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface IAdaptorUserRoleRepository : IRepository<AdaptorUserRole>
{
    AdaptorUserRole GetByRoleName(string roleName);
    Task<AdaptorUserRole> GetByRoleNameAsync(string roleName);
    AdaptorUserRole GetByRoleNames(IEnumerable<string> roleNames);
    Task<AdaptorUserRole> GetByRoleNamesAsync(IEnumerable<string> roleNames);
}