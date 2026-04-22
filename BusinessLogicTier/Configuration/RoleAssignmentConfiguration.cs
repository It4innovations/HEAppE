using System;
using System.Collections.Generic;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using Microsoft.Extensions.Logging;
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

    public static void AssignAllRolesFromConfig(AdaptorUserGroup group, IUnitOfWork unitOfWork, ILogger logger, bool doNotSave = false)
    {
        var rolesProcessed = new List<string>();
        int totalAssigned = 0;
        int totalMissing = 0;
        int totalAlreadyHad = 0;

        void Process(string[] usernames, AdaptorUserRoleType role)
        {
            if (usernames == null || usernames.Length == 0) return;

            var res = AssignSpecificRole(usernames, role, group, unitOfWork);
        
            if (res.Assigned.Any())
            {
                totalAssigned += res.Assigned.Count;
                rolesProcessed.Add(role.ToString());
            }
        
            totalMissing += res.Missing.Count;
            totalAlreadyHad += res.ExistingCount;
        }

        Process(Administrators, AdaptorUserRoleType.Administrator);
        Process(Maintainers, AdaptorUserRoleType.Maintainer);
        Process(Managers, AdaptorUserRoleType.Manager);
        Process(Submitters, AdaptorUserRoleType.Submitter);
        Process(Reporters, AdaptorUserRoleType.Reporter);
        Process(GroupReporters, AdaptorUserRoleType.GroupReporter);
        Process(ManagementAdmins, AdaptorUserRoleType.ManagementAdmin);

        if (totalAssigned > 0)
        {
            string rolesSummary = string.Join(", ", rolesProcessed.Distinct());
            logger.LogInformation($"Group '{group.Name}': Assigned {totalAssigned} new users to roles: {rolesSummary}");
        }

        if (totalMissing > 0)
        {
            logger.LogWarning($"Group '{group.Name}': {totalMissing} users defined in config were NOT FOUND in database.");
        }

        logger.LogDebug($"Group '{group.Name}' summary: {totalAssigned} new | {totalAlreadyHad} existing | {totalMissing} missing.");

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