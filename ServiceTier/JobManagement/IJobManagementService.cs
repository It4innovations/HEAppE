﻿using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ServiceTier.JobManagement
{
    public interface IJobManagementService
    {
        SubmittedJobInfoExt CreateJob(JobSpecificationExt specification, string sessionCode);
        SubmittedJobInfoExt SubmitJob(long createdJobInfoId, string sessionCode);
        SubmittedJobInfoExt CancelJob(long submittedJobInfoId, string sessionCode);
        void DeleteJob(long submittedJobInfoId, string sessionCode);
        SubmittedJobInfoExt[] ListJobsForCurrentUser(string sessionCode);
        SubmittedJobInfoExt GetCurrentInfoForJob(long submittedJobInfoId, string sessionCode);
        void CopyJobDataToTemp(long submittedJobInfoId, string sessionCode, string path);
        void CopyJobDataFromTemp(long createdJobInfoId, string sessionCode, string tempSessionCode);
        string[] GetAllocatedNodesIPs(long submittedJobInfoId, string sessionCode);
    }
}