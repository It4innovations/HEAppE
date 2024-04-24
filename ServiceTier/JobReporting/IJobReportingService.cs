using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.JobReporting.Models.DetailedReport;
using HEAppE.ExtModels.JobReporting.Models.ListReport;
using System;
using System.Collections.Generic;

namespace HEAppE.ServiceTier.JobReporting
{
    public interface IJobReportingService
    {
        IEnumerable<UserGroupListReportExt> ListAdaptorUserGroups(string sessionCode);
        IEnumerable<ProjectReportExt> UserResourceUsageReport(long userId, DateTime startTime, DateTime endTime,
            string[] subProjects, string sessionCode);
        ProjectReportExt UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime,
            string[] subProjects, string sessionCode);
        IEnumerable<ProjectAggregatedReportExt> AggregatedUserGroupResourceUsageReport(DateTime startTime, DateTime endTime, string sessionCode);
        ProjectExtendedReportExt ResourceUsageReportForJob(long jobId, string sessionCode);
        IEnumerable<JobStateAggregationReportExt> GetJobsStateAgregationReport(string sessionCode);
        IEnumerable<ProjectDetailedReportExt> JobsDetailedReport(string[] subProjects, string sessionCode);
    }
}
