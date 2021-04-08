using HEAppE.ExtModels.UserAndLimitationManagement.Models;

namespace HEAppE.ServiceTier.UserAndLimitationManagement
{
    public interface IUserAndLimitationManagementService
    {
        string AuthenticateUser(AuthenticationCredentialsExt credentials);
        OpenStackApplicationCredentialsExt AuthenticateUserToOpenStack(AuthenticationCredentialsExt credentials);
        ResourceUsageExt[] GetCurrentUsageAndLimitationsForCurrentUser(string sessionCode);

        //AdaptorUserGroupExt[] GetPossibleSubmitterGroupsForCurrentUser(string sessionCode);
    }
}