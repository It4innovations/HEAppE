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
            foreach (var userRoleGroup in adaptorUserUserGroupRoles?.GroupBy(x => x.AdaptorUserId))
            {
                var userRoles = userRoleGroup.ToList();
                if (IsRoleInCollection(userRoles, UserRoleType.Administrator))
                {
                    CheckAndAddUserUserRole(userRoles, UserRoleType.Maintainer, adaptorUserUserGroupRoles);
                    CheckAndAddUserUserRole(userRoles, UserRoleType.Reporter, adaptorUserUserGroupRoles);
                    CheckAndAddUserUserRole(userRoles, UserRoleType.Submitter, adaptorUserUserGroupRoles);
                }

                if (IsRoleInCollection(userRoles, UserRoleType.Submitter))
                {
                    CheckAndAddUserUserRole(userRoles, UserRoleType.Reporter, adaptorUserUserGroupRoles);
                }
            }
            return adaptorUserUserGroupRoles;
        }

        /// <summary>
        /// Checks and adds role to collection when is not in collection grouped by user
        /// </summary>
        /// <param name="currentUserUserGroupRoles">Collection of role to user mapping grouped by user</param>
        /// <param name="roleType">System role</param>
        /// <param name="adaptorUserUserGroupRoleCollection">Global role to user collection mapping</param>
        public static void CheckAndAddUserUserRole(IEnumerable<AdaptorUserUserGroupRole> currentUserUserGroupRoles, UserRoleType roleType, List<AdaptorUserUserGroupRole> adaptorUserUserGroupRoleCollection)
        {
            var userRole = currentUserUserGroupRoles.FirstOrDefault();
            if (!IsRoleInCollection(currentUserUserGroupRoles, roleType))
            {
                adaptorUserUserGroupRoleCollection.Add(new AdaptorUserUserGroupRole()
                {
                    AdaptorUserId = userRole.AdaptorUserId,
                    AdaptorUserRoleId = (long)roleType,
                    AdaptorUserGroupId = userRole.AdaptorUserId
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
    }
}
