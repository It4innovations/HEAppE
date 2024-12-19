using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.JobManagement;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.Exceptions.External;
using System;
using System.Text.RegularExpressions;

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
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, specification.ProjectId);
                IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                SubProject subProject = null;
                
                if (!string.IsNullOrEmpty(specification.SubProjectIdentifier))
                {
                    IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                    subProject = managementLogic.CreateSubProject(specification.SubProjectIdentifier, specification.ProjectId);
                }
                
                JobSpecification js = specification.ConvertExtToInt(specification.ProjectId, subProject?.Id);
                SubmittedJobInfo jobInfo = jobLogic.CreateJob(js, loggedUser, specification.IsExtraLong);
                return jobInfo.ConvertIntToExt();
            }
        }
        
        public SubmittedJobInfoExt SubmitJob(long createdJobInfoId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var job = unitOfWork.JobSpecificationRepository.GetById(createdJobInfoId) ?? throw new InputValidationException("NotExistingJob", createdJobInfoId);
                IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, job.ProjectId);
                SubmittedJobInfo jobInfo = jobLogic.SubmitJob(createdJobInfoId, loggedUser);
                return jobInfo.ConvertIntToExt();
            }
        }

        public SubmittedJobInfoExt CancelJob(long submittedJobInfoId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ?? throw new InputValidationException("NotExistingJob", submittedJobInfoId);
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, job.Project.Id);
                IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                SubmittedJobInfo jobInfo = jobLogic.CancelJob(submittedJobInfoId, loggedUser);
                return jobInfo.ConvertIntToExt();
            }
        }

        public bool DeleteJob(long submittedJobInfoId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ?? throw new InputValidationException("NotExistingJob", submittedJobInfoId);
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, job.Project.Id);
                IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                return jobLogic.DeleteJob(submittedJobInfoId, loggedUser);
            }
        }

        public SubmittedJobInfoExt[] ListJobsForCurrentUser(string sessionCode, string jobStates = null)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, var projects) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter);
                IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                var jobInfos = jobLogic.GetJobsForUser(loggedUser);
                var result = jobInfos.Select(s => s.ConvertIntToExt()).ToArray();
                if (jobStates != null)
                {
                    var values = Enum.GetValues(typeof(JobStateExt));
                    Array.Reverse(values);
                    foreach (JobStateExt st in values)
                    {
                        jobStates = jobStates.Replace(((int)st).ToString(),st.ToString());
                    }
                    jobStates = Regex.Replace(jobStates.Replace(" ", ""), @",+", ",").Trim(',');
                    if (!Enum.TryParse(jobStates, true, out JobStateExt statesComb))
                    {
                        throw new InputValidationException("JobStatesInvalid", jobStates);
                    }
                    result = result.Where(x => ((int)statesComb & (int)x.State) != 0).ToArray();
                }
                return result;
            }
        }

        public SubmittedJobInfoExt CurrentInfoForJob(long submittedJobInfoId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ?? throw new InputValidationException("NotExistingJob", submittedJobInfoId);
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, job.Project.Id);

                IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                SubmittedJobInfo jobInfo = jobLogic.GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
                return jobInfo.ConvertIntToExt();
            }
        }

        public void CopyJobDataToTemp(long submittedJobInfoId, string sessionCode, string path)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ?? throw new InputValidationException("NotExistingJob", submittedJobInfoId);
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, job.Project.Id);
                IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);

                jobLogic.CopyJobDataToTemp(submittedJobInfoId, loggedUser, sessionCode, path);
            }
        }

        public void CopyJobDataFromTemp(long createdJobInfoId, string sessionCode, string tempSessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                JobSpecification job = unitOfWork.JobSpecificationRepository.GetById(createdJobInfoId) ?? throw new InputValidationException("NotExistingJob", createdJobInfoId);
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, job.Project.Id);
                IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);

                jobLogic.CopyJobDataFromTemp(createdJobInfoId, loggedUser, tempSessionCode);
            }
        }

        public IEnumerable<string> AllocatedNodesIPs(long submittedTaskInfoId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var task = unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskInfoId);
                if (task is null)
                {
                    throw new InputValidationException("NotExistingTask", submittedTaskInfoId);
                }
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, task.Project.Id);
                IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                var nodesIPs = jobLogic.GetAllocatedNodesIPs(submittedTaskInfoId, loggedUser);

                return nodesIPs.ToArray();
            }
        }
        #endregion
    }
}