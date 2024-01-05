using System.Collections.Generic;

namespace HEAppE.DomainObjects.UserAndLimitationManagement.Enums;

public static class UserRoleTypeExtensions
{
    public static IEnumerable<UserRoleType> GetAllowedRolesForUserRoleType(this UserRoleType userRoleType)
    {
        switch (userRoleType)
        {
            case UserRoleType.Administrator:
                return new List<UserRoleType> { UserRoleType.Administrator };
            case UserRoleType.Maintainer:
                return new List<UserRoleType> { UserRoleType.Maintainer, UserRoleType.Administrator };
            case UserRoleType.Submitter:
                return new List<UserRoleType> { UserRoleType.Submitter, UserRoleType.Maintainer, UserRoleType.Administrator };
            case UserRoleType.GroupReporter:
                return new List<UserRoleType> { UserRoleType.GroupReporter, UserRoleType.Submitter, UserRoleType.Maintainer, UserRoleType.Administrator };
            case UserRoleType.Reporter:
                return new List<UserRoleType> { UserRoleType.Reporter, UserRoleType.GroupReporter, UserRoleType.Submitter, UserRoleType.Maintainer, UserRoleType.Administrator };
            case UserRoleType.ManagementAdmin:
                return new List<UserRoleType> { UserRoleType.ManagementAdmin, UserRoleType.Administrator };
            default:
                return new List<UserRoleType>();
        }
    }
}