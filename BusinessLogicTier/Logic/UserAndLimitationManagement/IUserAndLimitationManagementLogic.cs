using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.DomainObjects.UserAndLimitationManagement.Wrapper;
using HEAppE.OpenStackAPI.DTO;

namespace HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement {
	public interface IUserAndLimitationManagementLogic {
		AdaptorUser GetUserForSessionCode(string sessionCode);
		Task<string> AuthenticateUserAsync(AuthenticationCredentials credentials);
		Task<AdaptorUser> AuthenticateUserToOpenIdAsync(OpenIdCredentials credentials);
		Task<ApplicationCredentialsDTO> AuthenticateOpenIdUserToOpenStackAsync(AdaptorUser adaptorUser, long projectId);
		IList<ResourceUsage> GetCurrentUsageAndLimitationsForUser(AdaptorUser loggedUser);
		bool AuthorizeUserForJobInfo(AdaptorUser loggedUser, SubmittedJobInfo jobInfo);
		bool AuthorizeUserForTaskInfo(AdaptorUser loggedUser, SubmittedTaskInfo taskInfo);
		AdaptorUserGroup GetDefaultSubmitterGroup(AdaptorUser loggedUser);
        IEnumerable<ProjectReference> GetProjectsForCurrentUser(AdaptorUser loggedUser);
    }
}