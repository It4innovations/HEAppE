using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using log4net;

namespace HEAppE.BusinessLogicTier.Configuration;

/// <summary>
/// Role assignments configuration
/// </summary>
public class RoleAssignmentConfiguration
{
   /// <summary>
   /// Administrators role assignments
   /// </summary>
   public static string[] Administrators { get; set; } = ["admin"];
   /// <summary>
   /// Maintainers role assignments
   /// </summary>
   public static string[] Maintainers { get; set; }
   /// <summary>
   /// Managers role assignments
   /// </summary>
   public static string[] Managers { get; set; }
   /// <summary>
   /// Submitters role assignments
   /// </summary>
   public static string[] Submitters { get; set; }
   /// <summary>
   /// Group reporters role assignments
   /// </summary>
   public static string[] GroupReporters { get; set; }
   /// <summary>
   /// Reporters role assignments
   /// </summary>
   public static string[] Reporters { get; set; }
   /// <summary>
   /// Management admins role assignments
   /// </summary>
   public static string[] ManagementAdmins { get; set; }
   
   public static void AssignAllRolesFromConfig(AdaptorUserGroup group, IUnitOfWork unitOfWork, ILog logger)
    {
        AssignSpecificRole(RoleAssignmentConfiguration.Administrators, AdaptorUserRoleType.Administrator, group, unitOfWork, logger);
        AssignSpecificRole(RoleAssignmentConfiguration.Maintainers, AdaptorUserRoleType.Maintainer, group,  unitOfWork, logger);
        AssignSpecificRole(RoleAssignmentConfiguration.Managers, AdaptorUserRoleType.Manager, group, unitOfWork, logger);
        AssignSpecificRole(RoleAssignmentConfiguration.Submitters, AdaptorUserRoleType.Submitter, group, unitOfWork, logger);
        AssignSpecificRole(RoleAssignmentConfiguration.Reporters, AdaptorUserRoleType.Reporter, group, unitOfWork, logger);
        AssignSpecificRole(RoleAssignmentConfiguration.GroupReporters, AdaptorUserRoleType.GroupReporter, group, unitOfWork, logger);
        AssignSpecificRole(RoleAssignmentConfiguration.ManagementAdmins, AdaptorUserRoleType.ManagementAdmin, group, unitOfWork, logger);
        unitOfWork.Save();
    }
    
    /// <summary>
    /// Assigns specific role to users in the provided usernames array
    /// </summary>
    /// <param name="usernames"></param>
    /// <param name="roleType"></param>
    /// <param name="group"></param>
    private static void AssignSpecificRole(string[] usernames, AdaptorUserRoleType roleType, AdaptorUserGroup group, IUnitOfWork unitOfWork, ILog logger)
    {
        if (usernames == null || usernames.Length == 0) return;

        foreach (var username in usernames)
        {
            var user = unitOfWork.AdaptorUserRepository.GetByName(username);
            if (user != null)
            {
                user.CreateSpecificUserRoleForUser(group, roleType);
                unitOfWork.AdaptorUserRepository.Update(user);
                
                logger.Info($"SysUser '{username}' assigned to role '{roleType}'.");
            }
            else
            {
                logger.Warn($"SysUser '{username}' found in config but not in Database");
            }
        }
    }
}