using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.DomainObjects.UserAndLimitationManagement.Wrapper;
using HEAppE.OpenStackAPI.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement
{
    public interface IUserAndLimitationManagementLogic
    {
        AdaptorUser GetUserForSessionCode(string sessionCode);
        Task<string> AuthenticateUserAsync(AuthenticationCredentials credentials);
        Task<AdaptorUser> AuthenticateUserToOpenIdAsync(OpenIdCredentials credentials);
        Task<ApplicationCredentialsDTO> AuthenticateOpenIdUserToOpenStackAsync(AdaptorUser adaptorUser, long projectId);
        IList<ResourceUsage> GetCurrentUsageAndLimitationsForUser(AdaptorUser loggedUser, DomainObjects.JobManagement.Project[] projects);
        IList<ProjectResourceUsage> CurrentUsageAndLimitationsForUserByProject(AdaptorUser loggedUser, DomainObjects.JobManagement.Project[] projects);
        bool AuthorizeUserForJobInfo(AdaptorUser loggedUser, SubmittedJobInfo jobInfo);
        bool AuthorizeUserForTaskInfo(AdaptorUser loggedUser, SubmittedTaskInfo taskInfo);
        AdaptorUserGroup GetDefaultSubmitterGroup(AdaptorUser loggedUser, long projectId);
        IEnumerable<ProjectReference> ProjectsForCurrentUser(AdaptorUser loggedUser, DomainObjects.JobManagement.Project[] projects);
    }
}