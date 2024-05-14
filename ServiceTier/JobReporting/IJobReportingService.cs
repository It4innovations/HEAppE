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
        IEnumerable<UserGroupReportExt> UserResourceUsageReport(long userId, DateTime startTime, DateTime endTime, string sessionCode);
        UserGroupReportExt UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime, string sessionCode);
        IEnumerable<UserGroupReportExt> AggregatedUserGroupResourceUsageReport(DateTime startTime, DateTime endTime, string sessionCode);
        ProjectReportExt ResourceUsageReportForJob(long jobId, string sessionCode);
        IEnumerable<JobStateAggregationReportExt> GetJobsStateAgregationReport(string sessionCode);
        IEnumerable<UserGroupDetailedReportExt> JobsDetailedReport(string sessionCode);
    }
}
