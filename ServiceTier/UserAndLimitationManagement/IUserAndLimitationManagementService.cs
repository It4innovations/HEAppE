using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HEAppE.ServiceTier.UserAndLimitationManagement
{
    public interface IUserAndLimitationManagementService
    {
        Task<string> AuthenticateUserAsync(AuthenticationCredentialsExt credentials);
        Task<OpenStackApplicationCredentialsExt> AuthenticateUserToOpenStackAsync(AuthenticationCredentialsExt credentials, long projectId);
        IEnumerable<ResourceUsageExt> GetCurrentUsageAndLimitationsForCurrentUser(string sessionCode);
        IEnumerable<ProjectReferenceExt> GetProjectsForCurrentUser(string sessionCode);
        bool ValidateUserPermissions(string sessionCode);
    }
}