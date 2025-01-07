using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement;

public interface IJobManagementLogic
{
    SubmittedJobInfo CreateJob(JobSpecification specification, AdaptorUser loggedUser, bool isExtraLong);
    SubmittedJobInfo SubmitJob(long createdJobInfoId, AdaptorUser loggedUser);
    SubmittedJobInfo CancelJob(long submittedJobInfoId, AdaptorUser loggedUser);
    bool DeleteJob(long submittedJobInfoId, AdaptorUser loggedUser);
    SubmittedJobInfo GetSubmittedJobInfoById(long submittedJobInfoId, AdaptorUser loggedUser);
    SubmittedTaskInfo GetSubmittedTaskInfoById(long submittedTaskInfoId, AdaptorUser loggedUser);
    IEnumerable<SubmittedJobInfo> GetJobsForUser(AdaptorUser loggedUser);
    IEnumerable<SubmittedJobInfo> GetNotFinishedJobInfosForSubmitterId(long submitterId);
    IEnumerable<SubmittedJobInfo> GetNotFinishedJobInfos();
    IEnumerable<SubmittedTaskInfo> GetAllFinishedTaskInfos(IEnumerable<long> taskIds);
    void UpdateCurrentStateOfUnfinishedJobs();
    void CopyJobDataToTemp(long submittedJobInfoId, AdaptorUser loggedUser, string hash, string path);
    void CopyJobDataFromTemp(long createdJobInfoId, AdaptorUser loggedUser, string hash);
    IEnumerable<string> GetAllocatedNodesIPs(long submittedTaskInfoId, AdaptorUser loggedUser);
}