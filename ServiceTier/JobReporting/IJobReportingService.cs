using System;
using System.Collections.Generic;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.JobReporting.Models.DetailedReport;
using HEAppE.ExtModels.JobReporting.Models.ListReport;

namespace HEAppE.ServiceTier.JobReporting;

public interface IJobReportingService
{
    IEnumerable<UserGroupListReportExt> ListAdaptorUserGroups(string sessionCode);

    IEnumerable<ProjectReportExt> UserResourceUsageReport(long userId, DateTime startTime, DateTime endTime,
        string[] subProjects, string sessionCode, int? limit = null, int? offset = null, long? clusterId = null);

    ProjectReportExt UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime,
        string[] subProjects, string sessionCode, int? limit = null, int? offset = null, long? clusterId = null, long? userId = null);

    IEnumerable<ProjectAggregatedReportExt> AggregatedUserGroupResourceUsageReport(DateTime startTime, DateTime endTime,
        string sessionCode, int? limit = null, int? offset = null, long? clusterId = null, long? userId = null);

    ProjectExtendedReportExt ResourceUsageReportForJob(long jobId, string sessionCode);
    IEnumerable<JobStateAggregationReportExt> GetJobsStateAgregationReport(string sessionCode);
    IEnumerable<ProjectDetailedReportExt> JobsDetailedReport(string[] subProjects, DateTime? timeFrom, DateTime? timeTo, string sessionCode,
        int? limit = null, int? offset = null, long? clusterId = null, long? userId = null);
}