using HEAppE.ExtModels.JobManagement.Models;
using System.Collections.Generic;

namespace HEAppE.ServiceTier.JobManagement
{
    public interface IJobManagementService
    {
        SubmittedJobInfoExt CreateJob(JobSpecificationByAccountingStringExt specification, string sessionCode);
        SubmittedJobInfoExt CreateJob(JobSpecificationByProjectExt specification, string sessionCode);
        SubmittedJobInfoExt SubmitJob(long createdJobInfoId, string sessionCode);
        SubmittedJobInfoExt CancelJob(long submittedJobInfoId, string sessionCode);
        void DeleteJob(long submittedJobInfoId, string sessionCode);
        SubmittedJobInfoExt[] ListJobsForCurrentUser(string sessionCode);
        SubmittedJobInfoExt CurrentInfoForJob(long submittedJobInfoId, string sessionCode);
        void CopyJobDataToTemp(long submittedJobInfoId, string sessionCode, string path);
        void CopyJobDataFromTemp(long createdJobInfoId, string sessionCode, string tempSessionCode);
        IEnumerable<string> AllocatedNodesIPs(long submittedTaskInfoId, string sessionCode);
    }
}