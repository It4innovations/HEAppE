using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.BusinessLogicTier.Logic.JobReporting;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.JobReporting.Converts;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.JobReporting.Models.DetailedReport;
using HEAppE.ExtModels.JobReporting.Models.ListReport;
using HEAppE.ExtModels.UserAndLimitationManagement.Converts;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Exceptions.External;

namespace HEAppE.ServiceTier.JobReporting
{
    public class JobReportingService : IJobReportingService
    {
        #region Instances
        /// <summary>
        /// Logger
        /// </summary>
        private static ILog _logger;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public JobReportingService()
        {
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }
        #endregion
        public IEnumerable<UserGroupListReportExt> ListAdaptorUserGroups(string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.GroupReporter, null);
                IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                return jobReportingLogic.UserGroupListReport().Select(s => s.ConvertIntToExt());
            }
        }

        public IEnumerable<UserGroupReportExt> UserResourceUsageReport(long userId, DateTime startTime, DateTime endTime, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.GroupReporter, null);

                IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                var reporterGroups = loggedUser.Groups.Select(x => x.Id).Distinct().ToList();
                return jobReportingLogic.UserResourceUsageReport(userId, reporterGroups, startTime, endTime).Select(g => g.ConvertIntToExt());
            }
        }

        public UserGroupReportExt UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter, null);
                var group = loggedUser.Groups.FirstOrDefault(val => val.Id == groupId);
                if (group == null)
                {
                    throw new NotAllowedException("Logged user is not allowed to request this report.");
                }

                IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                return jobReportingLogic.UserGroupResourceUsageReport(groupId, startTime, endTime).ConvertIntToExt();
            }
        }

        public IEnumerable<UserGroupReportExt> AggregatedUserGroupResourceUsageReport(DateTime startTime, DateTime endTime, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter, null);

                IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                List<long> userGroupIds = loggedUser.Groups.Select(val => val.Id).Distinct().ToList();
                return jobReportingLogic.AggregatedUserGroupResourceUsageReport(userGroupIds, startTime, endTime).Select(x => x.ConvertIntToExt()).ToList();
            }
        }

        public ProjectReportExt ResourceUsageReportForJob(long jobId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter, null);
                IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                List<long> userGroupIds = loggedUser.Groups.Select(val => val.Id).Distinct().ToList();
                return jobReportingLogic.ResourceUsageReportForJob(jobId, userGroupIds).ConvertIntToExt();
            }
        }

        public IEnumerable<JobStateAggregationReportExt> GetJobsStateAgregationReport(string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.GroupReporter, null);
                var reportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                return reportingLogic.AggregatedJobsByStateReport().Select(s => s.ConvertIntToExt());
            }
        }

        public IEnumerable<UserGroupDetailedReportExt> JobsDetailedReport(string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.GroupReporter, null);
                var reportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                List<long> userGroupIds = loggedUser.Groups.Select(val => val.Id).Distinct().ToList();
                return reportingLogic.JobsDetailedReport(userGroupIds).Select(s => s.ConvertIntToDetailedExt());
            }
        }
    }
}
