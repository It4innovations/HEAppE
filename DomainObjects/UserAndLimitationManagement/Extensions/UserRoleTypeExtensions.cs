using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace HEAppE.DomainObjects.UserAndLimitationManagement.Extensions
{
    public static class UserRoleTypeExtensions
    {
        private static readonly List<AdaptorUserRoleType> _administratorSubRoles = new()
        {  
            AdaptorUserRoleType.Reporter, 
            AdaptorUserRoleType.GroupReporter,
            AdaptorUserRoleType.Submitter,
            AdaptorUserRoleType.Maintainer,
            AdaptorUserRoleType.Manager,
            AdaptorUserRoleType.ManagementAdmin,
            AdaptorUserRoleType.Administrator 
        };

        private static readonly List<AdaptorUserRoleType> _managementAdminSubRoles = new()
        {
            AdaptorUserRoleType.ManagementAdmin
        };
        
        private static readonly List<AdaptorUserRoleType> _managerSubRoles = new()
        {
            AdaptorUserRoleType.Manager,
            AdaptorUserRoleType.Reporter,
            AdaptorUserRoleType.GroupReporter,
            AdaptorUserRoleType.Submitter
        };

        private static readonly List<AdaptorUserRoleType> _maintainerSubRoles = new() 
        { 
            AdaptorUserRoleType.Reporter,
            AdaptorUserRoleType.GroupReporter,
            AdaptorUserRoleType.Submitter,
            AdaptorUserRoleType.Manager,
            AdaptorUserRoleType.ManagementAdmin,
            AdaptorUserRoleType.Maintainer 
        };

        private static readonly List<AdaptorUserRoleType> _submitterSubRoles = new() 
        { 
            AdaptorUserRoleType.Reporter,
            AdaptorUserRoleType.GroupReporter,
            AdaptorUserRoleType.Submitter
        };

        private static readonly List<AdaptorUserRoleType> _groupReporterSubRoles = new() 
        { 
            AdaptorUserRoleType.Reporter,
            AdaptorUserRoleType.GroupReporter 
        };

        private static readonly List<AdaptorUserRoleType> _reporterSubRoles = new()
        {
            AdaptorUserRoleType.Reporter
        };

        public static IEnumerable<AdaptorUserRoleType> GetAllowedRolesForUserRoleType(this AdaptorUserRoleType userRoleType)
        {
            return userRoleType switch
            {
                AdaptorUserRoleType.Administrator => _administratorSubRoles,
                AdaptorUserRoleType.Maintainer => _maintainerSubRoles,
                AdaptorUserRoleType.Submitter => _submitterSubRoles,
                AdaptorUserRoleType.GroupReporter => _groupReporterSubRoles,
                AdaptorUserRoleType.Reporter => _reporterSubRoles,
                AdaptorUserRoleType.ManagementAdmin => _managementAdminSubRoles,
                AdaptorUserRoleType.Manager => _managerSubRoles,
                _ => new List<AdaptorUserRoleType>(),
            };
        }
    }
}
