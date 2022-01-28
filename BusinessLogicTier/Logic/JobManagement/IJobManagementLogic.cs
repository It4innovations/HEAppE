using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement {
	public interface IJobManagementLogic {
		SubmittedJobInfo CreateJob(JobSpecification specification, AdaptorUser loggedUser, bool isExtraLong);
		SubmittedJobInfo SubmitJob(long createdJobInfoId, AdaptorUser loggedUser);
		SubmittedJobInfo CancelJob(long submittedJobInfoId, AdaptorUser loggedUser);
        void DeleteJob(long submittedJobInfoId, AdaptorUser loggedUser);
        SubmittedJobInfo GetSubmittedJobInfoById(long submittedJobInfoId, AdaptorUser loggedUser);
		IList<SubmittedJobInfo> ListJobsForUser(AdaptorUser loggedUser);
		IList<SubmittedJobInfo> ListNotFinishedJobInfosForSubmitterId(long submitterId);
		IList<SubmittedJobInfo> ListNotFinishedJobInfos();
		IList<SubmittedJobInfo> UpdateCurrentStateOfUnfinishedJobs();
        void CopyJobDataToTemp(long submittedJobInfoId, AdaptorUser loggedUser, string hash, string path);
        void CopyJobDataFromTemp(long createdJobInfoId, AdaptorUser loggedUser, string hash);
        IEnumerable<string> GetAllocatedNodesIPs(long submittedJobInfoId, AdaptorUser loggedUser);
    }
}