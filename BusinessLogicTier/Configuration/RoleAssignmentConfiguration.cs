using System.Collections.Generic;
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

    public static void AssignAllRolesFromConfig(AdaptorUserGroup group, IUnitOfWork unitOfWork, ILog logger, bool doNotSave = false)
    {
        AssignSpecificRole(Administrators, AdaptorUserRoleType.Administrator, group, unitOfWork, logger);
        AssignSpecificRole(Maintainers, AdaptorUserRoleType.Maintainer, group, unitOfWork, logger);
        AssignSpecificRole(Managers, AdaptorUserRoleType.Manager, group, unitOfWork, logger);
        AssignSpecificRole(Submitters, AdaptorUserRoleType.Submitter, group, unitOfWork, logger);
        AssignSpecificRole(Reporters, AdaptorUserRoleType.Reporter, group, unitOfWork, logger);
        AssignSpecificRole(GroupReporters, AdaptorUserRoleType.GroupReporter, group, unitOfWork, logger);
        AssignSpecificRole(ManagementAdmins, AdaptorUserRoleType.ManagementAdmin, group, unitOfWork, logger);
        if (doNotSave) return;
        unitOfWork.Save();
    }

    private static void AssignSpecificRole(string[] usernames, AdaptorUserRoleType roleType, AdaptorUserGroup group, IUnitOfWork unitOfWork, ILog logger)
    {
        if (usernames == null || usernames.Length == 0) return;

        var distinctUsernames = new HashSet<string>(usernames);
        var assignedUsers = new List<string>();
        var missingUsers = new List<string>();
        var alreadyHadRoleCount = 0;

        foreach (var username in distinctUsernames)
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
                    assignedUsers.Add(username);
                }
                else
                {
                    alreadyHadRoleCount++;
                }
            }
            else
            {
                missingUsers.Add(username);
            }
        }
        
        if (assignedUsers.Any())
        {
            logger.Info($"SUCCESSfully assigned role '{roleType}' in group '{group.Name}' to: {string.Join(", ", assignedUsers)}");
        }

        if (missingUsers.Any())
        {
            logger.Warn($"MISSING in Database (found in config): {string.Join(", ", missingUsers)}");
        }

        if (alreadyHadRoleCount > 0 || assignedUsers.Any() || missingUsers.Any())
        {
            logger.Debug($"Summary for '{roleType}'/'{group.Name}': {assignedUsers.Count} new, {alreadyHadRoleCount} existing, {missingUsers.Count} missing.");
        }
    }
}