using HEAppE.DomainObjects.UserAndLimitationManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Exceptions.External
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
        /// Create InsufficientRoleException with prepared message.
        /// </summary>
        /// <param name="requiredRole">Required (missing) user role.</param>
        /// <param name="availableRoles">Available user roles of the user.</param>
        /// <returns>New InsufficientRoleException.</returns>
        public static InsufficientRoleException CreateMissingRoleException(AdaptorUserRole requiredRole, IEnumerable<AdaptorUserRole> availableRoles, long projectId)
        {
            return (availableRoles is null || availableRoles.Count() == 0) ? new InsufficientRoleException("MissingRole", requiredRole.Name, projectId) :
                new InsufficientRoleException("MissingRoles", requiredRole.Name, projectId, string.Join(",", availableRoles.Select(role => role.Name)));
        }
    }
}