using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HEAppE.ServiceTier.UserAndLimitationManagement
{
    public interface IUserAndLimitationManagementService
    {
        Task<string> AuthenticateUserAsync(AuthenticationCredentialsExt credentials);
        Task<OpenStackApplicationCredentialsExt> AuthenticateUserToOpenStackAsync(AuthenticationCredentialsExt credentials, long projectId);
        IEnumerable<ProjectResourceUsageExt> CurrentUsageAndLimitationsForCurrentUserByProject(string sessionCode);
        IEnumerable<ProjectReferenceExt> ProjectsForCurrentUser(string sessionCode);
        bool ValidateUserPermissions(string sessionCode, AdaptorUserRoleType requestedRole);
        AdaptorUserExt GetCurrentUserInfo(string sessionCode);
    }
}