using HEAppE.DomainObjects.UserAndLimitationManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement.Exceptions
{
    public class InsufficientRoleException : ExternallyVisibleException
    {
        public InsufficientRoleException(string message) : base(message)
        {
        }

        public InsufficientRoleException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Create InsufficientRoleException with prepared message.
        /// </summary>
        /// <param name="requiredRole">Required (missing) user role.</param>
        /// <param name="availableRoles">Available user roles of the user.</param>
        /// <returns>New InsufficientRoleException.</returns>
        public static InsufficientRoleException CreateMissingRoleException(AdaptorUserRole requiredRole, IEnumerable<AdaptorUserRole> availableRoles, long projectId)
        {
            string rolesForProjectText = (availableRoles is null || availableRoles.Count() == 0) ? $"Current user does not have any permission/role for project '{projectId}'." : $"Current user roles for project {projectId}: '{string.Join(",", availableRoles.Select(role => role.Name))}'.";
            string message = $"User doesn't have required role. Required role: '{requiredRole.Name}'. {rolesForProjectText}";
            return new InsufficientRoleException(message);
        }
    }
}