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
        /// <param name="userGroupRoles">List of AdaptorUserUserGroupRoles</param>
        /// <param name="userRoles">List of AdaptorUserRoles</param>
        /// <returns>List of AdaptorUserUserGroupRoles</returns>
        public static IEnumerable<AdaptorUserUserGroupRole> GetAllUserRoles(List<AdaptorUserUserGroupRole> userGroupRoles, IList<AdaptorUserRole> userRoles)
        {
            var groupRoles = new List<AdaptorUserUserGroupRole>();
            
            //group by User and UserGroup
            foreach (var userGroupRole in userGroupRoles.GroupBy(x => new { x.AdaptorUserId, x.AdaptorUserGroupId }))
            {
                var isRolePresentDictionary = userRoles.ToDictionary(x => x.Id, _ => false);

                foreach (var roleMapping in userGroupRole)
                {
                    if (isRolePresentDictionary[roleMapping.AdaptorUserRoleId])
                    {
                        continue;
                    }

                    groupRoles.Add(CreateUserGroupRole(roleMapping));
                    isRolePresentDictionary[roleMapping.AdaptorUserRoleId] = true;

                    var parentRoleId = userRoles.FirstOrDefault(x => x.Id == roleMapping.AdaptorUserRoleId)?.ParentRoleId;
                    //check if parent role is present
                    while (parentRoleId.HasValue && !isRolePresentDictionary[parentRoleId.Value])
                    {
                        groupRoles.Add(CreateUserGroupRole(roleMapping, parentRoleId.Value));
                        isRolePresentDictionary[parentRoleId.Value] = true;
                        parentRoleId = userRoles.FirstOrDefault(x => x.Id == parentRoleId.Value)?.ParentRoleId;
                    }
                }
            }

            return groupRoles.Distinct();
        }

        /// <summary>
        /// Helper method to create a new AdaptorUserUserGroupRole
        /// </summary>
        /// <param name="roleMapping"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        private static AdaptorUserUserGroupRole CreateUserGroupRole(AdaptorUserUserGroupRole roleMapping, long? roleId = null)
        {
            return new AdaptorUserUserGroupRole()
            {
                AdaptorUserId = roleMapping.AdaptorUserId,
                AdaptorUserGroupId = roleMapping.AdaptorUserGroupId,
                AdaptorUserRoleId = roleId ?? roleMapping.AdaptorUserRoleId
            };
        }
    }
}
