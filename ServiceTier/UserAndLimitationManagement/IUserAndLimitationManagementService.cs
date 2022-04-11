using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System.Threading.Tasks;

namespace HEAppE.ServiceTier.UserAndLimitationManagement
{
    public interface IUserAndLimitationManagementService
    {
        Task<string> AuthenticateUserAsync(AuthenticationCredentialsExt credentials);

        Task<OpenStackApplicationCredentialsExt> AuthenticateUserToOpenStackAsync(AuthenticationCredentialsExt credentials);

        ResourceUsageExt[] GetCurrentUsageAndLimitationsForCurrentUser(string sessionCode);
    }
}