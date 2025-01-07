using System.Collections.Generic;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ServiceTier.JobManagement;

public interface IJobManagementService
{
    SubmittedJobInfoExt CreateJob(JobSpecificationExt specification, string sessionCode);
    SubmittedJobInfoExt SubmitJob(long createdJobInfoId, string sessionCode);
    SubmittedJobInfoExt CancelJob(long submittedJobInfoId, string sessionCode);
    bool DeleteJob(long submittedJobInfoId, string sessionCode);
    SubmittedJobInfoExt[] ListJobsForCurrentUser(string sessionCode, string jobStates = null);
    SubmittedJobInfoExt CurrentInfoForJob(long submittedJobInfoId, string sessionCode);
    void CopyJobDataToTemp(long submittedJobInfoId, string sessionCode, string path);
    void CopyJobDataFromTemp(long createdJobInfoId, string sessionCode, string tempSessionCode);
    IEnumerable<string> AllocatedNodesIPs(long submittedTaskInfoId, string sessionCode);
}