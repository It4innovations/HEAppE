using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using log4net;
using System.Linq;

namespace HEAppE.BusinessLogicTier.Configuration;

public class RoleAssignmentConfiguration
{
    public static string[] Administrators { get; set; } = ["admin"];
    public static string[] Maintainers { get; set; }
    public static string[] Managers { get; set; }
    public static string[] Submitters { get; set; }
    public static string[] GroupReporters { get; set; }
    public static string[] Reporters { get; set; }
    public static string[] ManagementAdmins { get; set; }

    public static void AssignAllRolesFromConfig(AdaptorUserGroup group, IUnitOfWork unitOfWork, ILog logger)
    {
        AssignSpecificRole(Administrators, AdaptorUserRoleType.Administrator, group, unitOfWork, logger);
        AssignSpecificRole(Maintainers, AdaptorUserRoleType.Maintainer, group, unitOfWork, logger);
        AssignSpecificRole(Managers, AdaptorUserRoleType.Manager, group, unitOfWork, logger);
        AssignSpecificRole(Submitters, AdaptorUserRoleType.Submitter, group, unitOfWork, logger);
        AssignSpecificRole(Reporters, AdaptorUserRoleType.Reporter, group, unitOfWork, logger);
        AssignSpecificRole(GroupReporters, AdaptorUserRoleType.GroupReporter, group, unitOfWork, logger);
        AssignSpecificRole(ManagementAdmins, AdaptorUserRoleType.ManagementAdmin, group, unitOfWork, logger);
        unitOfWork.Save();
    }

    private static void AssignSpecificRole(string[] usernames, AdaptorUserRoleType roleType, AdaptorUserGroup group, IUnitOfWork unitOfWork, ILog logger)
    {
        if (usernames == null || usernames.Length == 0) return;

        foreach (var username in usernames)
        {
            var user = unitOfWork.AdaptorUserRepository.GetByName(username);
            if (user != null)
            {
                bool alreadyHasRole = user.AdaptorUserUserGroupRoles?.Any(r => 
                    r.AdaptorUserGroupId == group.Id && 
                    r.AdaptorUserRoleId == (long)roleType) ?? false;

                if (!alreadyHasRole)
                {
                    user.CreateSpecificUserRoleForUser(group, roleType);
                    unitOfWork.AdaptorUserRepository.Update(user);
                    logger.Info($"SysUser '{username}' assigned to role '{roleType}' in group '{group.Name}'.");
                }
            }
            else
            {
                logger.Warn($"SysUser '{username}' found in config but not in Database");
            }
        }
    }
}