using HEAppE.DomainObjects.JobReporting;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System;
using System.Collections.Generic;

namespace HEAppE.BusinessLogicTier.Logic.JobReporting
{
    public interface IJobReportingLogic
    {
        IEnumerable<UserGroupListReport> UserGroupListReport();
        IEnumerable<JobStateAggregationReport> AggregatedJobsByStateReport();
        IEnumerable<UserGroupReport> JobsDetailedReport(IEnumerable<long> groupIds);
        ProjectReport ResourceUsageReportForJob(long jobId);
        UserResourceUsageReport UserResourceUsageReport(long userId, DateTime startTime, DateTime endTime);
        UserGroupReport UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime);
        IEnumerable<UserGroupReport> AggregatedUserGroupResourceUsageReport(IEnumerable<long> groupIds, DateTime startTime, DateTime endTime);
    }
}