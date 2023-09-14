using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.Utils
{
    /// <summary>
    /// User role utils
    /// </summary>
    public static class UserRoleUtils
    {
        /// <summary>
        /// Returns all user to role mappings and adds cascade roles for specific roles
        /// </summary>
        /// <param name="adaptorUserUserGroupRoles"></param>
        /// <returns></returns>
        public static IEnumerable<AdaptorUserUserGroupRole> GetAllUserRoles(List<AdaptorUserUserGroupRole> adaptorUserUserGroupRoles)
        {
            List<AdaptorUserUserGroupRole> groupRoleCascadeAppender = new List<AdaptorUserUserGroupRole>(adaptorUserUserGroupRoles);
            foreach (var userGroupRole in adaptorUserUserGroupRoles)
            {
                var currentUserGroupRoles = GetUserRolesInGroup(adaptorUserUserGroupRoles, userGroupRole.AdaptorUserId, userGroupRole.AdaptorUserGroupId);

                if (IsRoleInCollection(currentUserGroupRoles, UserRoleType.HpcProjectAdmin))
                {
                    CheckAndAddMissingUserUserRole(userGroupRole, currentUserGroupRoles, UserRoleType.Administrator, groupRoleCascadeAppender);
                    CheckAndAddMissingUserUserRole(userGroupRole, currentUserGroupRoles, UserRoleType.Maintainer, groupRoleCascadeAppender);
                    CheckAndAddMissingUserUserRole(userGroupRole, currentUserGroupRoles, UserRoleType.Submitter, groupRoleCascadeAppender);
                    CheckAndAddMissingUserUserRole(userGroupRole, currentUserGroupRoles, UserRoleType.GroupReporter, groupRoleCascadeAppender);
                    CheckAndAddMissingUserUserRole(userGroupRole, currentUserGroupRoles, UserRoleType.Reporter, groupRoleCascadeAppender);
                }
                if (IsRoleInCollection(currentUserGroupRoles, UserRoleType.Administrator))
                {
                    CheckAndAddMissingUserUserRole(userGroupRole, currentUserGroupRoles, UserRoleType.Maintainer, groupRoleCascadeAppender);
                    CheckAndAddMissingUserUserRole(userGroupRole, currentUserGroupRoles, UserRoleType.Submitter, groupRoleCascadeAppender);
                    CheckAndAddMissingUserUserRole(userGroupRole, currentUserGroupRoles, UserRoleType.GroupReporter, groupRoleCascadeAppender);
                    CheckAndAddMissingUserUserRole(userGroupRole, currentUserGroupRoles, UserRoleType.Reporter, groupRoleCascadeAppender);
                }
                else if (IsRoleInCollection(currentUserGroupRoles, UserRoleType.Maintainer))
                {
                    CheckAndAddMissingUserUserRole(userGroupRole, currentUserGroupRoles, UserRoleType.Submitter, groupRoleCascadeAppender);
                    CheckAndAddMissingUserUserRole(userGroupRole, currentUserGroupRoles, UserRoleType.GroupReporter, groupRoleCascadeAppender);
                    CheckAndAddMissingUserUserRole(userGroupRole, currentUserGroupRoles, UserRoleType.Reporter, groupRoleCascadeAppender);
                }
                else if (IsRoleInCollection(currentUserGroupRoles, UserRoleType.Submitter))
                {
                    CheckAndAddMissingUserUserRole(userGroupRole, currentUserGroupRoles, UserRoleType.Reporter, groupRoleCascadeAppender);
                }
                else if (IsRoleInCollection(currentUserGroupRoles, UserRoleType.GroupReporter))
                {
                    CheckAndAddMissingUserUserRole(userGroupRole, currentUserGroupRoles, UserRoleType.Reporter, groupRoleCascadeAppender);
                }
            }
            return groupRoleCascadeAppender.Distinct();
        }

        /// <summary>
        /// Checks and adds role when is not in collection grouped by user
        /// </summary>
        /// <param name="userGroupRole">Current userGroup role from configuration</param>
        /// <param name="currentUserGroupRoles">All user roles in specific group</param>
        /// <param name="roleType">System role type</param>
        /// <param name="adaptorUserUserGroupRoleDBCollection">All user group roles for append</param>
        public static void CheckAndAddMissingUserUserRole(AdaptorUserUserGroupRole userGroupRole, List<AdaptorUserUserGroupRole> currentUserGroupRoles, UserRoleType roleType, List<AdaptorUserUserGroupRole> adaptorUserUserGroupRoleDBCollection)
        {
            if (!IsRoleInCollection(currentUserGroupRoles, roleType))
            {
                adaptorUserUserGroupRoleDBCollection.Add(new AdaptorUserUserGroupRole()
                {
                    AdaptorUserId = userGroupRole.AdaptorUserId,
                    AdaptorUserRoleId = (long)roleType,
                    AdaptorUserGroupId = userGroupRole.AdaptorUserGroupId
                });
            }
        }

        /// <summary>
        /// Checks if role is in collection by RoleId
        /// </summary>
        /// <param name="adaptorUserUserGroupRoles">Collection of roles</param>
        /// <param name="role">System role</param>
        /// <returns></returns>
        public static bool IsRoleInCollection(IEnumerable<AdaptorUserUserGroupRole> adaptorUserUserGroupRoles, UserRoleType role)
        {
            return adaptorUserUserGroupRoles.Any(x => x.AdaptorUserRoleId.Equals((long)role));
        }

        /// <summary>
        /// Returns user roles in specific group
        /// </summary>
        /// <param name="adaptorUserUserGroupRoles">All user group roles</param>
        /// <param name="userId">AdaptorUserId</param>
        /// <param name="groupId">AdaptorUserGroupId</param>
        /// <returns></returns>
        public static List<AdaptorUserUserGroupRole> GetUserRolesInGroup(List<AdaptorUserUserGroupRole> adaptorUserUserGroupRoles, long userId, long groupId)
        {
            return adaptorUserUserGroupRoles.Where(x => x.AdaptorUserId == userId && x.AdaptorUserGroupId == groupId).ToList();
        }
    }
}
