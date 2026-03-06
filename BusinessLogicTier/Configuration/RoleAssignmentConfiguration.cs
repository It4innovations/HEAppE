using System;
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
        var totalAssigned = new HashSet<string>();
        var totalMissing = new HashSet<string>();
        int totalAlreadyHad = 0;

        void Process(string[] usernames, AdaptorUserRoleType role)
        {
            var res = AssignSpecificRole(usernames, role, group, unitOfWork);
            foreach (var u in res.Assigned) totalAssigned.Add(u);
            foreach (var u in res.Missing) totalMissing.Add(u);
            totalAlreadyHad += res.ExistingCount;
        }

        Process(Administrators, AdaptorUserRoleType.Administrator);
        Process(Maintainers, AdaptorUserRoleType.Maintainer);
        Process(Managers, AdaptorUserRoleType.Manager);
        Process(Submitters, AdaptorUserRoleType.Submitter);
        Process(Reporters, AdaptorUserRoleType.Reporter);
        Process(GroupReporters, AdaptorUserRoleType.GroupReporter);
        Process(ManagementAdmins, AdaptorUserRoleType.ManagementAdmin);

        if (totalAssigned.Any())
            logger.Info($"Group '{group.Name}': SUCCESSfully assigned roles to: {string.Join(", ", totalAssigned)}");

        if (totalMissing.Any())
            logger.Warn($"Group '{group.Name}': MISSING users in DB: {string.Join(", ", totalMissing.Distinct())}");

        logger.Debug($"Group '{group.Name}' summary: {totalAssigned.Count} new, {totalAlreadyHad} existing, {totalMissing.Count} missing.");

        if (!doNotSave) unitOfWork.Save();
    }

    private static (List<string> Assigned, List<string> Missing, int ExistingCount) AssignSpecificRole(string[] usernames, AdaptorUserRoleType roleType, AdaptorUserGroup group, IUnitOfWork unitOfWork)
    {
        var assigned = new List<string>();
        var missing = new List<string>();
        int existingCount = 0;

        if (usernames == null || usernames.Length == 0) return (assigned, missing, existingCount);

        foreach (var username in new HashSet<string>(usernames))
        {
            var user = unitOfWork.AdaptorUserRepository.GetByName(username);
            if (user != null)
            {
                bool hasRole = user.AdaptorUserUserGroupRoles?.Any(r => 
                    r.AdaptorUserGroupId == group.Id && r.AdaptorUserRoleId == (long)roleType) ?? false;

                if (!hasRole)
                {
                    user.CreateSpecificUserRoleForUser(group, roleType);
                    unitOfWork.AdaptorUserRepository.Update(user);
                    assigned.Add(username);
                }
                else existingCount++;
            }
            else missing.Add(username);
        }
        return (assigned, missing, existingCount);
    }
}