using System;
using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting;

namespace HEAppE.BusinessLogicTier.Logic.JobReporting;

public interface IJobReportingLogic
{
    IEnumerable<UserGroupListReport> UserGroupListReport(IEnumerable<Project> projects, long userId);
    IEnumerable<JobStateAggregationReport> AggregatedJobsByStateReport(IEnumerable<Project> projects);
    IEnumerable<ProjectReport> JobsDetailedReport(IEnumerable<long> groupIds, string[] subProjects, DateTime? timeFrom, DateTime? timeTo,
        int? limit = null, int? offset = null, long? clusterId = null, long? userId = null);
    ProjectReport ResourceUsageReportForJob(long jobId, IEnumerable<long> reporterGroupIds);

    IEnumerable<ProjectReport> UserResourceUsageReport(long userId, IEnumerable<long> reporterGroupIds,
        DateTime startTime, DateTime endTime, string[] subProjects,
        int? limit = null, int? offset = null, long? clusterId = null);

    ProjectReport UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime,
        string[] subProjects,
        int? limit = null, int? offset = null, long? clusterId = null, long? userId = null);

    IEnumerable<ProjectAggregatedReport> AggregatedUserGroupResourceUsageReport(IEnumerable<long> groupIds,
        DateTime startTime, DateTime endTime,
        int? limit = null, int? offset = null, long? clusterId = null, long? userId = null);
}