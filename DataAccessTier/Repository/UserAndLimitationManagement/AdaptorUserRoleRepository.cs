using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.Internal;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement;

internal class AdaptorUserRoleRepository : GenericRepository<AdaptorUserRole>, IAdaptorUserRoleRepository
{
    #region Constructors

    internal AdaptorUserRoleRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public AdaptorUserRole GetByRoleName(string roleName)
    {
        return _context.AdaptorUserRoles.FirstOrDefault(f => roleName.Contains(f.Name));
    }

    public async Task<AdaptorUserRole> GetByRoleNameAsync(string roleName)
    {
        return await _context.AdaptorUserRoles.FirstOrDefaultAsync(f => roleName.Contains(f.Name));
    }

    public AdaptorUserRole GetByRoleNames(IEnumerable<string> roleNames)
    {
        var adaptorUserRoles = _dbSet.Where(w => roleNames.Contains(w.Name)).ToList();
        return ResolveRoleFromList(adaptorUserRoles);
    }

    public async Task<AdaptorUserRole> GetByRoleNamesAsync(IEnumerable<string> roleNames)
    {
        var adaptorUserRoles = await _dbSet.Where(w => roleNames.Contains(w.Name)).ToListAsync();
        return ResolveRoleFromList(adaptorUserRoles);
    }

    private static AdaptorUserRole ResolveRoleFromList(List<AdaptorUserRole> adaptorUserRoles)
    {
        return adaptorUserRoles switch
        {
            var role when role.Any(a => a.RoleType == AdaptorUserRoleType.Administrator) => role.First(f =>
                f.RoleType == AdaptorUserRoleType.Administrator),
            var role when role.Any(a => a.RoleType == AdaptorUserRoleType.ManagementAdmin) => role.First(f =>
                f.RoleType == AdaptorUserRoleType.ManagementAdmin),
            var role when role.Any(a => a.RoleType == AdaptorUserRoleType.Maintainer) => role.First(f =>
                f.RoleType == AdaptorUserRoleType.Maintainer),
            var role when role.Any(a => a.RoleType == AdaptorUserRoleType.Manager) => role.First(f =>
                f.RoleType == AdaptorUserRoleType.Manager),
            var role when role.Any(a => a.RoleType == AdaptorUserRoleType.Submitter) => role.First(f =>
                f.RoleType == AdaptorUserRoleType.Submitter),
            var role when role.Any(a => a.RoleType == AdaptorUserRoleType.GroupReporter) => role.First(f =>
                f.RoleType == AdaptorUserRoleType.GroupReporter),
            var role when role.Any(a => a.RoleType == AdaptorUserRoleType.Reporter) => role.First(f =>
                f.RoleType == AdaptorUserRoleType.Reporter),
            _ => throw new AdaptorUserGroupException("NoExist")
        };
    }

    #endregion
}