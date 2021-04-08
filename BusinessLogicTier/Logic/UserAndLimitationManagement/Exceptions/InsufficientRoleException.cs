using System;
using System.Linq;
using System.Collections.Generic;
using HEAppE.DomainObjects.UserAndLimitationManagement;

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
        internal static InsufficientRoleException CreateMissingRoleException(AdaptorUserRole requiredRole, IEnumerable<AdaptorUserRole> availableRoles)
        {
            string message = $"User doesn't have required role. Required role: '{requiredRole.Name}'. Current user roles: '{string.Join(",", availableRoles.Select(role => role.Name))}'";
            return new InsufficientRoleException(message);
        }
    }
}