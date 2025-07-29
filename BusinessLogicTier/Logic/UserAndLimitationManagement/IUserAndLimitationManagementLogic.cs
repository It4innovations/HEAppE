using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.DomainObjects.UserAndLimitationManagement.Wrapper;
using HEAppE.OpenStackAPI.DTO;

namespace HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;

public interface IUserAndLimitationManagementLogic
{
    AdaptorUser GetUserForSessionCode(string sessionCode);
    Task<string> AuthenticateUserAsync(AuthenticationCredentials credentials);
    Task<AdaptorUser> AuthenticateUserToOpenIdAsync(OpenIdCredentials credentials);
    Task<ApplicationCredentialsDTO> AuthenticateOpenIdUserToOpenStackAsync(AdaptorUser adaptorUser, long projectId);
    IList<ResourceUsage> GetCurrentUsageAndLimitationsForUser(AdaptorUser loggedUser, IEnumerable<Project> projects);

    IList<ProjectResourceUsage> CurrentUsageAndLimitationsForUserByProject(AdaptorUser loggedUser,
        IEnumerable<Project> projects);

    public Task<AdaptorUser> HandleTokenAsApiKeyAuthenticationAsync(LexisCredentials lexisCredentials);
    bool AuthorizeUserForJobInfo(AdaptorUser loggedUser, SubmittedJobInfo jobInfo);
    bool AuthorizeUserForTaskInfo(AdaptorUser loggedUser, SubmittedTaskInfo taskInfo);
    AdaptorUserGroup GetDefaultSubmitterGroup(AdaptorUser loggedUser, long projectId);
    IEnumerable<ProjectReference> ProjectsForCurrentUser(AdaptorUser loggedUser, IEnumerable<Project> projects);
}