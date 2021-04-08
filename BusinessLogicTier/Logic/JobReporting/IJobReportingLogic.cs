using HEAppE.DomainObjects.JobReporting;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System;
using System.Collections.Generic;

namespace HEAppE.BusinessLogicTier.Logic.JobReporting {
	public interface IJobReportingLogic {
        IList<AdaptorUserGroup> ListAdaptorUserGroups();
        SubmittedJobInfoUsageReport GetResourceUsageReportForJob(long jobId);
        UserResourceUsageReport GetUserResourceUsageReport(long userId, DateTime startTime, DateTime endTime);
        UserGroupResourceUsageReport GetUserGroupResourceUsageReport(long userId, long groupId, DateTime startTime, DateTime endTime);
    }
}