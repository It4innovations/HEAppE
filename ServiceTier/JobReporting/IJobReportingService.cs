using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.JobReporting.Models.DetailedReport;
using HEAppE.ExtModels.JobReporting.Models.ListReport;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System;
using System.Collections.Generic;

namespace HEAppE.ServiceTier.JobReporting
{
    public interface IJobReportingService
    {
        IEnumerable<UserGroupListReportExt> ListAdaptorUserGroups(string sessionCode);
        UserResourceReportExt GetUserResourceUsageReport(long userId, DateTime startTime, DateTime endTime, string sessionCode);
        UserGroupReportExt GetUserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime, string sessionCode);
        IEnumerable<UserGroupReportExt> GetAggregatedUserGroupResourceUsageReport(DateTime startTime, DateTime endTime, string sessionCode);
        ProjectReportExt GetResourceUsageReportForJob(long jobId, string sessionCode);
        IEnumerable<JobStateAggregationReportExt> GetJobsStateAgregationReport(string sessionCode);
        IEnumerable<UserGroupDetailedReportExt> GetJobsDetailedReport(string sessionCode);
    }
}
