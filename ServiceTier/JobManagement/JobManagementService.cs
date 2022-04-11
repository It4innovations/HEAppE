using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.BusinessLogicTier.Logic.JobManagement;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using System.Reflection;
using HEAppE.ExtModels.JobManagement.Converts;
using log4net;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement.Roles;

namespace HEAppE.ServiceTier.JobManagement
{
    public class JobManagementService : IJobManagementService
    {
        #region Instances
        private readonly ILog _logger;
        #endregion
        #region Constructors
        public JobManagementService()
        {
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }
        #endregion
        #region Methods
        public SubmittedJobInfoExt CreateJob(JobSpecificationExt specification, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    JobSpecification js = specification.ConvertExtToInt();
                    SubmittedJobInfo jobInfo = jobLogic.CreateJob(js, loggedUser, specification.IsExtraLong.Value);
                    return jobInfo.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public SubmittedJobInfoExt SubmitJob(long createdJobInfoId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    SubmittedJobInfo jobInfo = jobLogic.SubmitJob(createdJobInfoId, loggedUser);
                    return jobInfo.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public SubmittedJobInfoExt CancelJob(long submittedJobInfoId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    SubmittedJobInfo jobInfo = jobLogic.CancelJob(submittedJobInfoId, loggedUser);
                    return jobInfo.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public void DeleteJob(long submittedJobInfoId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    jobLogic.DeleteJob(submittedJobInfoId, loggedUser);
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
            }
        }

        public SubmittedJobInfoExt[] ListJobsForCurrentUser(string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    var jobInfos = jobLogic.GetJobsForUser(loggedUser);
                    return jobInfos.Select(s => s.ConvertIntToExt()).ToArray();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public SubmittedJobInfoExt GetCurrentInfoForJob(long submittedJobInfoId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    SubmittedJobInfo jobInfo = jobLogic.GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
                    return jobInfo.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public void CopyJobDataToTemp(long submittedJobInfoId, string sessionCode, string path)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    jobLogic.CopyJobDataToTemp(submittedJobInfoId, loggedUser, sessionCode, path);
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
            }
        }

        public void CopyJobDataFromTemp(long createdJobInfoId, string sessionCode, string tempSessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    jobLogic.CopyJobDataFromTemp(createdJobInfoId, loggedUser, tempSessionCode);
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
            }
        }

        public IEnumerable<string> GetAllocatedNodesIPs(long submittedTaskInfoId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    var nodesIPs = jobLogic.GetAllocatedNodesIPs(submittedTaskInfoId, loggedUser);
                    return nodesIPs.ToArray();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }
        #endregion
    }
}