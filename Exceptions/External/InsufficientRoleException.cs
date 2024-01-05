using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.Exceptions.AbstractTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.Exceptions.External
{
    public class InsufficientRoleException : ExternalException
    {
        public InsufficientRoleException(string message) : base(message)
        {
        }

        public InsufficientRoleException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public InsufficientRoleException(string message, params object[] args) : base(message, args)
        {
        }

        public InsufficientRoleException(string message, Exception innerException, params object[] args) : base(message, innerException, args)
        {
        }

        /// <summary>
        /// Create InsufficientRoleException with prepared message
        /// </summary>
        /// <param name="allowedRole">Allowed (missing) user roles</param>
        /// <param name="availableRoles">Available user roles of the user</param>
        /// <param name="projectId"></param>
        /// <returns>ew InsufficientRoleException</returns>
        public static InsufficientRoleException CreateMissingRoleException(IEnumerable<AdaptorUserRole> allowedRoles, IEnumerable<AdaptorUserRole> availableRoles, long projectId)
        {
            string allowedRolesString = string.Join(",", allowedRoles.Select(role => role.Name));
            return (availableRoles is null || availableRoles.Count() == 0) ? new InsufficientRoleException("MissingRole", allowedRolesString, projectId) :
                new InsufficientRoleException("MissingRoles", allowedRolesString, projectId, string.Join(",", availableRoles.Select(role => role.Name)));
        }

        /// <summary>
        /// Create InsufficientRoleException with prepared message.
        /// </summary>
        /// <param name="requiredRole">Required (missing) user role.</param>
        /// <returns>New InsufficientRoleException.</returns>
        public static InsufficientRoleException CreateMissingRoleException(AdaptorUserRole requiredRole)
        {
            return new InsufficientRoleException("MissingRoleForProjectCreation", requiredRole.Name);
        }
    }
}