using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.OpenStackAPI.DTO;

namespace HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement {
	public interface IUserAndLimitationManagementLogic {
		AdaptorUser GetUserForSessionCode(string sessionCode);
		string AuthenticateUser(AuthenticationCredentials credentials);
		ApplicationCredentialsDTO AuthenticateUserToOpenStack(AuthenticationCredentials credentials);
		IList<ResourceUsage> GetCurrentUsageAndLimitationsForUser(AdaptorUser loggedUser);
		bool AuthorizeUserForJobInfo(AdaptorUser loggedUser, SubmittedJobInfo jobInfo);
		AdaptorUserGroup GetDefaultSubmitterGroup(AdaptorUser loggedUser);
	}
}