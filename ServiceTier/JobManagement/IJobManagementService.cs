using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ServiceTier.JobManagement;

public interface IJobManagementService
{
    Task<SubmittedJobInfoExt> CreateJob(JobSpecificationExt specification, string sessionCode);
    SubmittedJobInfoExt SubmitJob(long createdJobInfoId, string sessionCode);
    Task<SubmittedJobInfoExt> GetActualTasksInfo(long submittedJobInfoId, string sessionCode);
    Task<SubmittedJobInfoExt> CancelJob(long submittedJobInfoId, string sessionCode);
    bool DeleteJob(long submittedJobInfoId, bool archiveLogs, string sessionCode);
    SubmittedJobInfoExt[] ListJobsForCurrentUser(string sessionCode, string jobStates = null);
    Task<SubmittedJobInfoExt> CurrentInfoForJob(long submittedJobInfoId, string sessionCode);
    void CopyJobDataToTemp(long createdJobInfoId, string sessionCode, string path);
    void CopyJobDataFromTemp(long createdJobInfoId, string sessionCode, string tempSessionCode);
    IEnumerable<string> AllocatedNodesIPs(long submittedTaskInfoId, string sessionCode);
    Task<DryRunJobInfoExt> DryRunJob(long modelProjectId, long modelClusterNodeTypeId, long modelNodes,
        long modelTasksPerNode, long modelWallTimeInMinutes, string modelSessionCode);
}