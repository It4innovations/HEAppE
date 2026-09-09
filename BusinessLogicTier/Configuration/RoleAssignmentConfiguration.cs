using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.Management;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.BusinessLogicTier.Configuration;

public class RoleAssignmentConfiguration
{
    private static readonly ConcurrentDictionary<AdaptorUserRoleType, ConcurrentDictionary<string, byte>> _dynamicRoleAssignments = new();

    public static string[] Administrators { get; set; }
    public static string[] Maintainers { get; set; }
    public static string[] Managers { get; set; }
    public static string[] Submitters { get; set; }
    public static string[] GroupReporters { get; set; }
    public static string[] Reporters { get; set; }
    public static string[] ManagementAdmins { get; set; }

    public static string[] GetConfiguredUsersForRole(AdaptorUserRoleType role)
    {
        var users = role switch
        {
            AdaptorUserRoleType.Administrator => Administrators ?? Array.Empty<string>(),
            AdaptorUserRoleType.Maintainer => Maintainers ?? Array.Empty<string>(),
            AdaptorUserRoleType.Manager => Managers ?? Array.Empty<string>(),
            AdaptorUserRoleType.Submitter => Submitters ?? Array.Empty<string>(),
            AdaptorUserRoleType.GroupReporter => GroupReporters ?? Array.Empty<string>(),
            AdaptorUserRoleType.Reporter => Reporters ?? Array.Empty<string>(),
            AdaptorUserRoleType.ManagementAdmin => ManagementAdmins ?? Array.Empty<string>(),
            _ => Array.Empty<string>()
        };
        return users.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static void AddDynamicRoleAssignment(string username, AdaptorUserRoleType role)
    {
        if (string.IsNullOrWhiteSpace(username)) return;
        var dict = _dynamicRoleAssignments.GetOrAdd(role, _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
        dict[username] = 0;
    }

    public static void RemoveDynamicRoleAssignment(string username, AdaptorUserRoleType role)
    {
        if (string.IsNullOrWhiteSpace(username)) return;
        if (_dynamicRoleAssignments.TryGetValue(role, out var dict))
        {
            dict.TryRemove(username, out _);
        }
    }

    public static string[] GetUsersForRole(AdaptorUserRoleType role)
    {
        var configured = GetConfiguredUsersForRole(role);
        if (_dynamicRoleAssignments.TryGetValue(role, out var dict) && !dict.IsEmpty)
        {
            return configured.Concat(dict.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
        return configured;
    }

    public static string GetRoleAssignmentSource(string username, AdaptorUserRoleType role)
    {
        bool inConfig = GetConfiguredUsersForRole(role).Any(u => string.Equals(u, username, StringComparison.OrdinalIgnoreCase));
        bool inDynamic = _dynamicRoleAssignments.TryGetValue(role, out var dict) && dict.ContainsKey(username);

        if (inConfig && inDynamic) return "Both";
        if (inDynamic) return "Dynamic";
        if (inConfig) return "Appsettings";
        return "None";
    }

    public static void LoadDynamicRoleAssignments(IEnumerable<SystemRoleAssignment> assignments)
    {
        if (assignments == null) return;
        _dynamicRoleAssignments.Clear();
        foreach (var assignment in assignments)
        {
            if (!string.IsNullOrWhiteSpace(assignment.Username))
            {
                AddDynamicRoleAssignment(assignment.Username, assignment.Role);
            }
        }
    }

    public static List<SystemRoleAssignment> GetAllRoleAssignments()
    {
        var result = new List<SystemRoleAssignment>();
        var allRoles = (AdaptorUserRoleType[])Enum.GetValues(typeof(AdaptorUserRoleType));

        foreach (var role in allRoles)
        {
            var users = GetUsersForRole(role);
            foreach (var user in users)
            {
                result.Add(new SystemRoleAssignment
                {
                    Username = user,
                    Role = role,
                    Source = GetRoleAssignmentSource(user, role)
                });
            }
        }

        return result;
    }

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

        Process(GetUsersForRole(AdaptorUserRoleType.Administrator), AdaptorUserRoleType.Administrator);
        Process(GetUsersForRole(AdaptorUserRoleType.Maintainer), AdaptorUserRoleType.Maintainer);
        Process(GetUsersForRole(AdaptorUserRoleType.Manager), AdaptorUserRoleType.Manager);
        Process(GetUsersForRole(AdaptorUserRoleType.Submitter), AdaptorUserRoleType.Submitter);
        Process(GetUsersForRole(AdaptorUserRoleType.Reporter), AdaptorUserRoleType.Reporter);
        Process(GetUsersForRole(AdaptorUserRoleType.GroupReporter), AdaptorUserRoleType.GroupReporter);
        Process(GetUsersForRole(AdaptorUserRoleType.ManagementAdmin), AdaptorUserRoleType.ManagementAdmin);

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
            var user = unitOfWork.AdaptorUserRepository.GetByNameIgnoreQueryFilters(username);
            if (user != null && !user.IsDeleted)
            {
                bool hasRole = user.AdaptorUserUserGroupRoles?.Any(r => 
                    r.AdaptorUserGroupId == group.Id && r.AdaptorUserRoleId == (long)roleType && !r.IsDeleted) ?? false;

                if (!hasRole)
                {
                    user.CreateSpecificUserRoleForUser(group, roleType);
                    unitOfWork.AdaptorUserRepository.Update(user);
                    assigned.Add(username);
                }
                else existingCount++;
            }
            else if (user == null) missing.Add(username);
        }
        return (assigned, missing, existingCount);
    }

    public static void AssignAllRolesFromConfigToAllGroups(List<AdaptorUserGroup> groups, IUnitOfWork unitOfWork, ILogger logger)
    {
        var allConfiguredUsernames = new HashSet<string>(
            GetUsersForRole(AdaptorUserRoleType.Administrator)
            .Concat(GetUsersForRole(AdaptorUserRoleType.Maintainer))
            .Concat(GetUsersForRole(AdaptorUserRoleType.Manager))
            .Concat(GetUsersForRole(AdaptorUserRoleType.Submitter))
            .Concat(GetUsersForRole(AdaptorUserRoleType.Reporter))
            .Concat(GetUsersForRole(AdaptorUserRoleType.GroupReporter))
            .Concat(GetUsersForRole(AdaptorUserRoleType.ManagementAdmin))
            .Where(x => !string.IsNullOrEmpty(x)),
            StringComparer.OrdinalIgnoreCase
        );

        if (!allConfiguredUsernames.Any())
        {
            logger.LogInformation("No roles defined in configuration. Synchronization skipped.");
            return;
        }

        // Fetch all these users in a single query with their group roles pre-loaded
        var users = unitOfWork.AdaptorUserRepository.GetQueryableWithoutFilters()
            .Include(x => x.AdaptorUserUserGroupRoles)
            .Where(u => allConfiguredUsernames.Contains(u.Username))
            .ToList();

        var userMap = users.ToDictionary(u => u.Username, u => u, StringComparer.OrdinalIgnoreCase);
        int totalAssigned = 0;

        foreach (var group in groups)
        {
            int assignedForGroup = 0;

            void Process(string[] usernames, AdaptorUserRoleType roleType)
            {
                if (usernames == null || usernames.Length == 0) return;

                foreach (var username in new HashSet<string>(usernames))
                {
                    if (userMap.TryGetValue(username, out var user))
                    {
                        if (!user.IsDeleted)
                        {
                            bool hasRole = user.AdaptorUserUserGroupRoles?.Any(r => 
                                r.AdaptorUserGroupId == group.Id && r.AdaptorUserRoleId == (long)roleType && !r.IsDeleted) ?? false;

                            if (!hasRole)
                            {
                                user.CreateSpecificUserRoleForUser(group, roleType);
                                unitOfWork.AdaptorUserRepository.Update(user);
                                assignedForGroup++;
                            }
                        }
                    }
                }
            }

            Process(GetUsersForRole(AdaptorUserRoleType.Administrator), AdaptorUserRoleType.Administrator);
            Process(GetUsersForRole(AdaptorUserRoleType.Maintainer), AdaptorUserRoleType.Maintainer);
            Process(GetUsersForRole(AdaptorUserRoleType.Manager), AdaptorUserRoleType.Manager);
            Process(GetUsersForRole(AdaptorUserRoleType.Submitter), AdaptorUserRoleType.Submitter);
            Process(GetUsersForRole(AdaptorUserRoleType.Reporter), AdaptorUserRoleType.Reporter);
            Process(GetUsersForRole(AdaptorUserRoleType.GroupReporter), AdaptorUserRoleType.GroupReporter);
            Process(GetUsersForRole(AdaptorUserRoleType.ManagementAdmin), AdaptorUserRoleType.ManagementAdmin);

            totalAssigned += assignedForGroup;
        }

        if (totalAssigned > 0)
        {
            logger.LogInformation($"Saving {totalAssigned} role assignments to database.");
            unitOfWork.Save();
        }
    }
}