using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using System.Collections.Generic;

namespace HEAppE.DomainObjects.UserAndLimitationManagement.Extensions
{
    public static class UserRoleTypeExtensions
    {
        public static IEnumerable<AdaptorUserRoleType> GetAllowedRolesForUserRoleType(this AdaptorUserRoleType userRoleType)
        {
            switch (userRoleType)
            {
                case AdaptorUserRoleType.Administrator:
                    return new List<AdaptorUserRoleType> { AdaptorUserRoleType.Administrator };
                case AdaptorUserRoleType.Maintainer:
                    return new List<AdaptorUserRoleType> { AdaptorUserRoleType.Maintainer, AdaptorUserRoleType.Administrator };
                case AdaptorUserRoleType.Submitter:
                    return new List<AdaptorUserRoleType> { AdaptorUserRoleType.Submitter, AdaptorUserRoleType.Maintainer, AdaptorUserRoleType.Administrator };
                case AdaptorUserRoleType.GroupReporter:
                    return new List<AdaptorUserRoleType> { AdaptorUserRoleType.GroupReporter, AdaptorUserRoleType.Submitter, AdaptorUserRoleType.Maintainer, AdaptorUserRoleType.Administrator };
                case AdaptorUserRoleType.Reporter:
                    return new List<AdaptorUserRoleType> { AdaptorUserRoleType.Reporter, AdaptorUserRoleType.GroupReporter, AdaptorUserRoleType.Submitter, AdaptorUserRoleType.Maintainer, AdaptorUserRoleType.Administrator };
                case AdaptorUserRoleType.ManagementAdmin:
                    return new List<AdaptorUserRoleType> { AdaptorUserRoleType.ManagementAdmin, AdaptorUserRoleType.Administrator };
                default:
                    return new List<AdaptorUserRoleType>();
            }
        }
    }
}
