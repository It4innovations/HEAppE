using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HEAppE.ServiceTier.UserAndLimitationManagement
{
    public interface IUserAndLimitationManagementService
    {
        Task<string> AuthenticateUserAsync(AuthenticationCredentialsExt credentials);
        Task<OpenStackApplicationCredentialsExt> AuthenticateUserToOpenStackAsync(AuthenticationCredentialsExt credentials);
        IEnumerable<ResourceUsageExt> GetCurrentUsageAndLimitationsForCurrentUser(string sessionCode);
        bool ValidateUserPermissions(string sessionCode);
    }
}