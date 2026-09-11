using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;

using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement;

public interface IJobManagementLogic
{
    Task<SubmittedJobInfo> CreateJob(JobSpecification specification, AdaptorUser loggedUser, bool isExtraLong);
    Task<SubmittedJobInfo> CreateJobDbRecord(JobSpecification specification, AdaptorUser loggedUser, bool isExtraLong);
    Task DeleteJobDbRecord(long jobInfoId, long specificationId);

    Task<SubmittedJobInfo> SubmitJobAsync(long createdJobInfoId, AdaptorUser loggedUser);
    Task<(SubmittedJobInfo JobInfo, bool IsWaitingForServiceAccount)> PrepareJobForSubmitAsync(long createdJobInfoId, AdaptorUser loggedUser);
    Task<SubmittedJobInfo> CompleteJobSubmitAsync(long createdJobInfoId, AdaptorUser loggedUser, IEnumerable<SubmittedTaskInfo> submittedTasks);

    Task<SubmittedJobInfo> GetActualTasksInfo(long submittedJobInfoId, AdaptorUser loggedUser);
    Task<(SubmittedJobInfo JobInfo, ClusterAuthenticationCredentials Credentials)> PrepareGetActualTasksInfoAsync(long submittedJobInfoId, AdaptorUser loggedUser);
    Task<SubmittedJobInfo> CompleteGetActualTasksInfoAsync(long submittedJobInfoId, AdaptorUser loggedUser, IEnumerable<SubmittedTaskInfo> actualTasksInfo);

    Task<SubmittedJobInfo> CancelJob(long submittedJobInfoId, AdaptorUser loggedUser);
    Task<(SubmittedJobInfo JobInfo, ClusterAuthenticationCredentials Credentials, bool CancelledLocally)> PrepareCancelJobAsync(long submittedJobInfoId, AdaptorUser loggedUser);
    Task<SubmittedJobInfo> CompleteCancelJobAsync(long submittedJobInfoId, AdaptorUser loggedUser, IEnumerable<SubmittedTaskInfo> actualTasksInfo);

    Task<bool> DeleteJobAsync(long submittedJobInfoId, AdaptorUser loggedUser);
    Task<(SubmittedJobInfo JobInfo, ClusterProject ClusterProject)> PrepareDeleteJobAsync(long submittedJobInfoId, AdaptorUser loggedUser);
    Task<bool> CompleteDeleteJobAsync(long submittedJobInfoId, AdaptorUser loggedUser, bool isDeleted);

    Task<bool> ArchiveJobAsync(long submittedJobInfoId, AdaptorUser loggedUser);
    Task<(SubmittedJobInfo JobInfo, string LocalBasePath, string JobLogArchivePath, IEnumerable<System.Tuple<string, string>> SourceDestinations)> PrepareArchiveJobAsync(long submittedJobInfoId, AdaptorUser loggedUser);
    SubmittedJobInfo GetSubmittedJobInfoById(long submittedJobInfoId, AdaptorUser loggedUser, bool isAdminOverride = false);
    Task<SubmittedJobInfo> GetSubmittedJobInfoByIdAsync(long submittedJobInfoId, AdaptorUser loggedUser, bool isAdminOverride = false);
    /// <summary>Lightweight status read - uses a minimal DB query, no SSH-related includes.</summary>
    SubmittedJobInfo GetSubmittedJobInfoByIdForStatus(long submittedJobInfoId, AdaptorUser loggedUser, bool isAdminOverride = false);
    Task<SubmittedJobInfo> GetSubmittedJobInfoByIdForStatusAsync(long submittedJobInfoId, AdaptorUser loggedUser, bool isAdminOverride = false);
    Task<SubmittedJobInfo> GetSubmittedJobInfoByIdForSubmitAsync(long submittedJobInfoId, AdaptorUser loggedUser, bool isAdminOverride = false);
    SubmittedTaskInfo GetSubmittedTaskInfoById(long submittedTaskInfoId, AdaptorUser loggedUser, bool checkSharedJobInfoAccess = false);
    Task<SubmittedTaskInfo> GetSubmittedTaskInfoByIdAsync(long submittedTaskInfoId, AdaptorUser loggedUser, bool checkSharedJobInfoAccess = false);
    IEnumerable<SubmittedJobInfo> GetJobsForUser(AdaptorUser loggedUser);
    Task<IEnumerable<SubmittedJobInfo>> GetJobsForUserAsync(AdaptorUser loggedUser);
    IEnumerable<SubmittedJobInfo> GetNotFinishedJobInfosForSubmitterId(long submitterId);
    Task<IEnumerable<SubmittedJobInfo>> GetNotFinishedJobInfosForSubmitterIdAsync(long submitterId);
    IEnumerable<SubmittedJobInfo> GetNotFinishedJobInfos();
    Task<IEnumerable<SubmittedJobInfo>> GetNotFinishedJobInfosAsync();
    IEnumerable<SubmittedTaskInfo> GetAllFinishedTaskInfos(IEnumerable<long> taskIds);
    Task<IEnumerable<SubmittedTaskInfo>> GetAllFinishedTaskInfosAsync(IEnumerable<long> taskIds);
    Task UpdateCurrentStateOfUnfinishedJobs();

    Task CopyJobDataToTempAsync(long createdJobInfoId, AdaptorUser loggedUser, string hash, string path);
    Task<(SubmittedJobInfo JobInfo, ClusterProject ClusterProject)> PrepareCopyJobDataToTempAsync(long createdJobInfoId, AdaptorUser loggedUser);

    Task CopyJobDataFromTempAsync(long createdJobInfoId, AdaptorUser loggedUser, string hash);
    Task<(SubmittedJobInfo JobInfo, ClusterProject ClusterProject)> PrepareCopyJobDataFromTempAsync(long createdJobInfoId, AdaptorUser loggedUser);

    Task<IEnumerable<string>> GetAllocatedNodesIPsAsync(long submittedTaskInfoId, AdaptorUser loggedUser);
    Task<SubmittedTaskInfo> PrepareGetAllocatedNodesIPsAsync(long submittedTaskInfoId, AdaptorUser loggedUser);

    Task<DryRunJobInfo> DryRunJob(long modelProjectId, long modelClusterNodeTypeId, long modelNodes,
        long modelTasksPerNode, long modelWallTimeInMinutes, AdaptorUser loggedUser);
    Task<(DryRunJobSpecification Specification, Cluster Cluster, Project Project)> PrepareDryRunJobAsync(long modelProjectId, long modelClusterNodeTypeId, long modelNodes, long modelTasksPerNode, long modelWallTimeInMinutes, AdaptorUser loggedUser);

    IQueryable<SubmittedJobInfo> GetJobsForUserQuery(long loggedUserId);
    Task<long> ProcessTaskCallbackAsync(string scheduledJobId, string token, string? rawResponse, string? qSchedulerState);
    Task<long> OpenQSchedulerSessionAsync(long clusterId, long projectId, string machineId, int walltimeLimitSecs, AdaptorUser loggedUser);
    Task CloseQSchedulerSessionAsync(long clusterId, long projectId, long sessionId, AdaptorUser loggedUser);
    Task<QSchedulerSession> GetQSchedulerSessionInfoAsync(long sessionId, AdaptorUser loggedUser);
    Task<System.Collections.Generic.IEnumerable<QSchedulerSession>> ListQSchedulerSessionsAsync(AdaptorUser loggedUser, QSchedulerSessionState? state = null, long? clusterId = null, long? projectId = null);
    Task<System.IO.Stream> GetQuantumTaskResultAsync(long submittedTaskId, AdaptorUser loggedUser);
    Task<System.IO.Stream> GetQuantumTaskArtifactAsync(long submittedTaskId, string artifactName, AdaptorUser loggedUser);
}