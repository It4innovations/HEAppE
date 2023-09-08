using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.BusinessLogicTier.Logic.JobReporting;
using HEAppE.BusinessLogicTier.Logic.JobReporting.Exceptions;
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
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    (AdaptorUser loggedUser, var projects) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.GroupReporter);
                    IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    return jobReportingLogic.UserGroupListReport(projects).Select(s => s.ConvertIntToExt());
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public IEnumerable<UserGroupReportExt> UserResourceUsageReport(long userId, DateTime startTime, DateTime endTime, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    (AdaptorUser loggedUser, var _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.GroupReporter);

                    IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    var reporterGroups = loggedUser.Groups.Select(x => x.Id).Distinct().ToList();
                    return jobReportingLogic.UserResourceUsageReport(userId, reporterGroups, startTime, endTime).Select(g => g.ConvertIntToExt());
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public UserGroupReportExt UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    (AdaptorUser loggedUser, var projects) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter);
                    var group = loggedUser.Groups.FirstOrDefault(val => val.Id == groupId);
                    if (group == null)
                    {
                        throw new NotAllowedException("Logged user is not allowed to request this report.");
                    }

                    if (!projects.Any(x => x.Id == group.ProjectId))
                    {
                        throw new NotAllowedException("Logged user is not allowed to request this report.");
                    }
                    IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    return jobReportingLogic.UserGroupResourceUsageReport(groupId, startTime, endTime).ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public IEnumerable<UserGroupReportExt> AggregatedUserGroupResourceUsageReport(DateTime startTime, DateTime endTime, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    (AdaptorUser loggedUser, var projects) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter);

                    IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    var userGroupIds = loggedUser.Groups.Select(x => x.Id).Distinct().ToList();

                    //get only groups which are in projects which are allowed for logged user
                    var reportAllowedGroupIds = userGroupIds.Where(g => projects.Any(project => project.Id == loggedUser.Groups.FirstOrDefault(group => group.Id == g).ProjectId)).ToList();

                    return jobReportingLogic.AggregatedUserGroupResourceUsageReport(reportAllowedGroupIds, startTime, endTime).Select(x => x.ConvertIntToExt()).ToList();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public ProjectReportExt ResourceUsageReportForJob(long jobId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    (AdaptorUser loggedUser, var _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter);
                    IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    List<long> userGroupIds = loggedUser.Groups.Select(val => val.Id).Distinct().ToList();
                    return jobReportingLogic.ResourceUsageReportForJob(jobId, userGroupIds).ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public IEnumerable<JobStateAggregationReportExt> GetJobsStateAgregationReport(string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    (AdaptorUser loggedUser, var projects) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.GroupReporter);
                    var reportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    return reportingLogic.AggregatedJobsByStateReport(projects).Select(s => s.ConvertIntToExt());
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public IEnumerable<UserGroupDetailedReportExt> JobsDetailedReport(string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    (AdaptorUser loggedUser, var projects) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.GroupReporter);
                    var reportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    List<long> userGroupIds = loggedUser.Groups.Select(val => val.Id).Distinct().ToList();

                    //get only groups which are in projects which are allowed for logged user
                    var reportAllowedGroupIds = userGroupIds.Where(g => projects.Any(project => project.Id == loggedUser.Groups.FirstOrDefault(group => group.Id == g).ProjectId)).ToList();

                    return reportingLogic.JobsDetailedReport(reportAllowedGroupIds).Select(s => s.ConvertIntToDetailedExt());
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }
    }
}
