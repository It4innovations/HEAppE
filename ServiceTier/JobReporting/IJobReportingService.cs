using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System;

namespace HEAppE.ServiceTier.JobReporting
{
    public interface IJobReportingService
    {
        AdaptorUserGroupExt[] ListAdaptorUserGroups(string sessionCode);
        UserResourceUsageReportExt GetUserResourceUsageReport(long userId, DateTime startTime, DateTime endTime, string sessionCode);
        UserGroupResourceUsageReportExt GetUserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime, string sessionCode);
        SubmittedJobInfoUsageReportExt GetResourceUsageReportForJob(long jobId, string sessionCode);
    }
}
