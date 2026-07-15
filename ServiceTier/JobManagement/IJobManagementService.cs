using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ServiceTier.JobManagement;

public interface IJobManagementService
{
    Task<SubmittedJobInfoExt> CreateJob(JobSpecificationExt specification, string sessionCode);
    Task<SubmittedJobInfoExt> SubmitJobAsync(long createdJobInfoId, string sessionCode);
    Task<SubmittedJobInfoExt> GetActualTasksInfo(long submittedJobInfoId, string sessionCode);
    Task<SubmittedJobInfoExt> CancelJob(long submittedJobInfoId, string sessionCode);
    Task<bool> DeleteJob(long submittedJobInfoId, bool archiveLogs, string sessionCode);
    Task<SubmittedJobInfoExt[]> ListJobsForCurrentUser(string sessionCode, string jobStates = null, int? limit = null, int? offset = null, long? userId = null, long? clusterId = null, long? subProjectId = null, long? projectId = null);
    Task<SubmittedJobInfoExt> CurrentInfoForJob(long submittedJobInfoId, string sessionCode);
    Task CopyJobDataToTempAsync(long createdJobInfoId, string sessionCode, string path);
    Task CopyJobDataFromTempAsync(long createdJobInfoId, string sessionCode, string tempSessionCode);
    Task<IEnumerable<string>> AllocatedNodesIPsAsync(long submittedTaskInfoId, string sessionCode);
    Task<DryRunJobInfoExt> DryRunJob(long modelProjectId, long modelClusterNodeTypeId, long modelNodes,
        long modelTasksPerNode, long modelWallTimeInMinutes, string modelSessionCode);
    Task ProcessTaskCallbackAsync(string scheduledJobId, string token, string? rawResponse, string? qSchedulerState);
    Task<long> OpenQSchedulerSessionAsync(long clusterId, long projectId, string machineId, int walltimeLimitSecs, string sessionCode);
    Task CloseQSchedulerSessionAsync(long clusterId, long projectId, long sessionId, string sessionCode);
    Task<QSchedulerSessionInfoExt> GetQSchedulerSessionInfoAsync(long sessionId, string sessionCode);
    Task<SubmittedJobInfoExt> CreateAndSubmitQSchedulerJob(QSchedulerJobSpecificationExt specification, string sessionCode);
    Task<IEnumerable<QSchedulerSessionInfoExt>> ListQSchedulerSessionsAsync(string sessionCode, string state = null, long? clusterId = null, long? projectId = null);
    Task<System.IO.Stream> GetQuantumTaskResultAsync(long submittedTaskId, string sessionCode);
    Task<System.IO.Stream> GetQuantumTaskArtifactAsync(long submittedTaskId, string artifactName, string sessionCode);
}