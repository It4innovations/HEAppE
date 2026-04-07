using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement;

public interface IJobManagementLogic
{
    Task<SubmittedJobInfo> CreateJob(JobSpecification specification, AdaptorUser loggedUser, bool isExtraLong);
    Task<SubmittedJobInfo> SubmitJob(long createdJobInfoId, AdaptorUser loggedUser);
    Task<SubmittedJobInfo> GetActualTasksInfo(long submittedJobInfoId, AdaptorUser loggedUser);
    Task<SubmittedJobInfo> CancelJob(long submittedJobInfoId, AdaptorUser loggedUser);
    Task<bool> DeleteJob(long submittedJobInfoId, AdaptorUser loggedUser);
    Task<bool> ArchiveJob(long submittedJobInfoId, AdaptorUser loggedUser);
    SubmittedJobInfo GetSubmittedJobInfoById(long submittedJobInfoId, AdaptorUser loggedUser, bool isAdminOverride = false);
    SubmittedTaskInfo GetSubmittedTaskInfoById(long submittedTaskInfoId, AdaptorUser loggedUser, bool checkSharedJobInfoAccess = false);
    IEnumerable<SubmittedJobInfo> GetJobsForUser(AdaptorUser loggedUser);
    IEnumerable<SubmittedJobInfo> GetNotFinishedJobInfosForSubmitterId(long submitterId);
    IEnumerable<SubmittedJobInfo> GetNotFinishedJobInfos();
    IEnumerable<SubmittedTaskInfo> GetAllFinishedTaskInfos(IEnumerable<long> taskIds);
    Task UpdateCurrentStateOfUnfinishedJobs();
    Task CopyJobDataToTemp(long createdJobInfoId, AdaptorUser loggedUser, string hash, string path);
    Task CopyJobDataFromTemp(long createdJobInfoId, AdaptorUser loggedUser, string hash);
    Task<IEnumerable<string>> GetAllocatedNodesIPs(long submittedTaskInfoId, AdaptorUser loggedUser);
    Task<DryRunJobInfo> DryRunJob(long modelProjectId, long modelClusterNodeTypeId, long modelNodes,
        long modelTasksPerNode, long modelWallTimeInMinutes, AdaptorUser loggedUser);
    IQueryable<SubmittedJobInfo> GetJobsForUserQuery(long loggedUserId);
}