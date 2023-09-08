﻿using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting;
using System;
using System.Collections.Generic;

namespace HEAppE.BusinessLogicTier.Logic.JobReporting
{
    public interface IJobReportingLogic
    {
        IEnumerable<UserGroupListReport> UserGroupListReport(Project[] projects);
        IEnumerable<JobStateAggregationReport> AggregatedJobsByStateReport(Project[] projects);
        IEnumerable<UserGroupReport> JobsDetailedReport(IEnumerable<long> groupIds);
        ProjectReport ResourceUsageReportForJob(long jobId, IEnumerable<long> reporterGroupIds);
        IEnumerable<UserGroupReport> UserResourceUsageReport(long userId, IEnumerable<long> reporterGroupIds, DateTime startTime, DateTime endTime);
        UserGroupReport UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime);
        IEnumerable<UserGroupReport> AggregatedUserGroupResourceUsageReport(IEnumerable<long> groupIds, DateTime startTime, DateTime endTime);
    }
}