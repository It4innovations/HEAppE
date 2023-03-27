using HEAppE.DomainObjects.JobReporting;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System;
using System.Collections.Generic;

namespace HEAppE.BusinessLogicTier.Logic.JobReporting
{
    public interface IJobReportingLogic
    {
        IEnumerable<UserGroupListReport> GetUserGroupListReport();
        IEnumerable<JobStateAggregationReport> GetAggregatedJobsByStateReport();
        IEnumerable<UserGroupReport> GetJobsDetailedReport(IEnumerable<long> groupIds);
        ProjectReport GetResourceUsageReportForJob(long jobId);
        UserResourceUsageReport GetUserResourceUsageReport(long userId, DateTime startTime, DateTime endTime);
        UserGroupReport GetUserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime);
        IEnumerable<UserGroupReport> GetAggregatedUserGroupResourceUsageReport(IEnumerable<long> groupIds, DateTime startTime, DateTime endTime);
    }
}