using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting;
using System;
using System.Collections.Generic;

namespace HEAppE.BusinessLogicTier.Logic.JobReporting
{
    public interface IJobReportingLogic
    {
        IEnumerable<UserGroupListReport> UserGroupListReport(IEnumerable<Project> projects, long userId);
        IEnumerable<JobStateAggregationReport> AggregatedJobsByStateReport(IEnumerable<Project> projects);
        IEnumerable<ProjectReport> JobsDetailedReport(IEnumerable<long> groupIds, string[] subProjects);
        ProjectReport ResourceUsageReportForJob(long jobId, IEnumerable<long> reporterGroupIds);
        IEnumerable<ProjectReport> UserResourceUsageReport(long userId, IEnumerable<long> reporterGroupIds,
            DateTime startTime, DateTime endTime, string[] subProjects);
        ProjectReport UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime, string[] subProjects);
        IEnumerable<ProjectAggregatedReport> AggregatedUserGroupResourceUsageReport(IEnumerable<long> groupIds, DateTime startTime, DateTime endTime);
    }
}