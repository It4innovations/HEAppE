using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SshCaAPI;

namespace HEAppE.ServiceTier.JobManagement;

public class JobManagementService : IJobManagementService
{
    #region Instances

    private readonly ILogger _logger;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IUserOrgService _userOrgService;
    private readonly IExpirioService _expirioService;

    #endregion

    #region Constructors

    public JobManagementService(IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys, IExpirioService expirioService, ILogger logger)
    {
        _userOrgService = userOrgService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _expirioService = expirioService;
        _logger = logger;
    }

    #endregion

    #region Methods

    public async Task<SubmittedJobInfoExt> CreateJob(JobSpecificationExt specification, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, specification.ProjectId, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            SubProject subProject = null;

            if (!string.IsNullOrEmpty(specification.SubProjectIdentifier))
            {
                var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
                subProject =
                    managementLogic.CreateSubProject(specification.SubProjectIdentifier, specification.ProjectId);
            }

            var js = specification.ConvertExtToInt(specification.ProjectId, subProject?.Id);
            var jobInfo = await jobLogic.CreateJob(js, loggedUser, specification.IsExtraLong);
            return jobInfo.ConvertIntToExt();
        }
    }

    public async Task<SubmittedJobInfoExt> SubmitJobAsync(long createdJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(createdJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", createdJobInfoId);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, job.Project.Id, _expirioService);
            var jobInfo = await jobLogic.SubmitJobAsync(createdJobInfoId, loggedUser);
            return jobInfo.ConvertIntToExt();
        }
    }

    public async Task<SubmittedJobInfoExt> GetActualTasksInfo(long submittedJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", submittedJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, job.Project.Id, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var jobInfo =  await jobLogic.GetActualTasksInfo(submittedJobInfoId, loggedUser);
            return jobInfo.ConvertIntToExt();
        }
    }

    public async Task<SubmittedJobInfoExt> CancelJob(long submittedJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", submittedJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, job.Project.Id, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var jobInfo = await jobLogic.CancelJob(submittedJobInfoId, loggedUser);
            return jobInfo.ConvertIntToExt();
        }
    }

    public async Task<bool> DeleteJob(long submittedJobInfoId, bool archiveLogs, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", submittedJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, job.Project.Id, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            if (archiveLogs)
            {
                _logger.LogInformation($"Archiving job logs {submittedJobInfoId} by user {loggedUser.Id}");
                await jobLogic.ArchiveJob(submittedJobInfoId, loggedUser);
            }
            return await jobLogic.DeleteJob(submittedJobInfoId, loggedUser);
        }
    }

    public SubmittedJobInfoExt[] ListJobsForCurrentUser(
        string sessionCode,
        string jobStates = null,
        int? limit = null,
        int? offset = null,
        long? userId = null,
        long? clusterId = null,
        long? subProjectId = null,
        long? projectId = null)
    {
        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger);

        var (loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(
            sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _logger, AdaptorUserRoleType.Submitter, _expirioService);

        var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(
            unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);

        bool isPrivileged = loggedUser.AdaptorUserUserGroupRoles.Any(r =>
            r.AdaptorUserRole != null &&
            r.AdaptorUserRole.ContainedRoleTypes != null &&
            (r.AdaptorUserRole.ContainedRoleTypes.Contains(AdaptorUserRoleType.Administrator) ||
             r.AdaptorUserRole.ContainedRoleTypes.Contains(AdaptorUserRoleType.Manager)));

        IQueryable<SubmittedJobInfo> query;
        if (isPrivileged)
        {
            query = unitOfWork.SubmittedJobInfoRepository.GetJobsQuery();
            if (userId.HasValue)
            {
                query = query.Where(x => x.Submitter.Id == userId.Value);
            }
        }
        else
        {
            query = jobLogic.GetJobsForUserQuery(loggedUser.Id);
        }

        query = query.AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Specification) // This is for the Job
            .Include(x => x.Project)       // This is for the Job
            .Include(x => x.Tasks)
            .ThenInclude(t => t.NodeType)
            .Include(x => x.Tasks) 
            .ThenInclude(t => t.Project)
            .Include(x => x.Tasks)        
            .ThenInclude(t => t.Specification); // Load Specification for each Task

        if (clusterId.HasValue)
        {
            query = query.Where(x => x.Specification.ClusterId == clusterId.Value);
        }

        if (subProjectId.HasValue)
        {
            query = query.Where(x => x.Specification.SubProjectId == subProjectId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(x => x.Project.Id == projectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(jobStates))
        {
            var stateInts = jobStates.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.Parse(s.Trim()))
                .ToList();
            
            query = query.Where(x => stateInts.Contains((int)x.State));
        }

        query = query.OrderByDescending(x => x.Id);

        if (offset.HasValue)
        {
            query = query.Skip(offset.Value);
        }

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return query
            .ToList() 
            .Select(x => x.ConvertIntToExt())
            .ToArray();
    }


    public async Task<SubmittedJobInfoExt> CurrentInfoForJob(long submittedJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", submittedJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, job.Project.Id, _expirioService);

            long projectId = job.Project?.Id ?? 0;
            //check if user is Admin
            bool isAdmin = UserAndLimitationManagementService.CheckIfUserHasRoleForProject(loggedUser, AdaptorUserRoleType.Administrator, projectId, true);
            bool isJobOwner = job.Submitter.Id == loggedUser.Id;
            
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            if (JwtTokenIntrospectionConfiguration.IsEnabled && isJobOwner && (job.State == JobState.Running || job.State == JobState.Queued))
            {
                var jobInfoFromHPC = await jobLogic.GetActualTasksInfo(submittedJobInfoId, loggedUser);
                return jobInfoFromHPC.ConvertIntToExt();
            }
            var jobInfo = jobLogic.GetSubmittedJobInfoById(submittedJobInfoId, loggedUser, isAdmin);
            return jobInfo.ConvertIntToExt();
        }
    }

    public async Task CopyJobDataToTempAsync(long createdJobInfoId, string sessionCode, string path)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(createdJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", createdJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, job.Project.Id, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);

            await jobLogic.CopyJobDataToTempAsync(createdJobInfoId, loggedUser, sessionCode, path);
        }
    }

    public async Task CopyJobDataFromTempAsync(long createdJobInfoId, string sessionCode, string tempSessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(createdJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", createdJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, job.Project.Id, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);

            await jobLogic.CopyJobDataFromTempAsync(createdJobInfoId, loggedUser, tempSessionCode);
        }
    }

    public async Task<IEnumerable<string>> AllocatedNodesIPsAsync(long submittedTaskInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var task = unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskInfoId);
            if (task is null) throw new InputValidationException("NotExistingTask", submittedTaskInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, task.Project.Id, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var nodesIPs = await jobLogic.GetAllocatedNodesIPsAsync(submittedTaskInfoId, loggedUser);

            return nodesIPs.ToArray();
        }
    }

    public async Task<DryRunJobInfoExt> DryRunJob(long modelProjectId, long modelClusterNodeTypeId, long modelNodes,
        long modelTasksPerNode,
        long modelWallTimeInMinutes, string modelSessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, modelProjectId, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var dryRunResult = (await jobLogic.DryRunJob(modelProjectId, modelClusterNodeTypeId, modelNodes,
                modelTasksPerNode, modelWallTimeInMinutes, loggedUser)).ConvertIntToExt();
            return dryRunResult;
        }
    }

    public async Task UpdateJobStatusFromCallback(string schedulerJobId, string payload)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            await jobLogic.UpdateJobStatusFromCallback(schedulerJobId, payload);
        }
    }

    #endregion
}