using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.DataAccessTier.Vault;
using SshCaAPI;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.BusinessLogicTier.Logic.JobManagement;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.BusinessLogicTier.Configuration;
using SshCaAPI.Configuration;

namespace HEAppE.ServiceTier.JobManagement;

public class JobManagementService : IJobManagementService
{
    #region Instances

    private readonly ILogger _logger;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IUserOrgService _userOrgService;
    private readonly IExpirioService _expirioService;
    private readonly IMemoryCache _cache;
    
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> _jobSemaphores = new();

    #endregion

    #region Constructors

    public JobManagementService(IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys, IExpirioService expirioService, IMemoryCache cache, ILogger logger)
    {
        _userOrgService = userOrgService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _expirioService = expirioService;
        _cache = cache;
        _logger = logger;
    }

    #endregion

    #region Methods

    public async Task<SubmittedJobInfoExt> CreateJob(JobSpecificationExt specification, string sessionCode)
    {
        SubmittedJobInfo jobInfo;
        JobSpecification js;
        AdaptorUser loggedUser;
        ClusterProject clusterProject;

        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, specification.ProjectId, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            SubProject subProject = null;

            if (!string.IsNullOrEmpty(specification.SubProjectIdentifier))
            {
                var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
                subProject =
                    managementLogic.CreateSubProject(specification.SubProjectIdentifier, specification.ProjectId);
            }

            js = specification.ConvertExtToInt(specification.ProjectId, subProject?.Id);
            jobInfo = await jobLogic.CreateJobDbRecord(js, loggedUser, specification.IsExtraLong);
            
            // Reload jobInfo with eager loading to prevent LazyLoadOnDisposedContextWarning when accessed outside unitOfWork
            jobInfo = unitOfWork.SubmittedJobInfoRepository.GetByIdWithTasks(jobInfo.Id) ?? jobInfo;
            
            clusterProject = unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(
                jobInfo.Specification.ClusterId, jobInfo.Project.Id)
                ?? throw new InvalidRequestException("NotExistingProject");
        } // unitOfWork is disposed here, connection is released!

        try
        {
            // SSH call: Create job directory
            await SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
                .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService,
                    adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
                .CreateJobDirectoryAsync(jobInfo, clusterProject.ScratchStoragePath,
                    BusinessLogicConfiguration.SharedAccountsPoolMode,
                    _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to create job directory for job {jobInfo.Id}. Cleaning up job specification and submitted job info.");
            try
            {
                using (var unitOfWorkCleanup = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
                {
                    var jobLogicCleanup = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWorkCleanup, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
                    await jobLogicCleanup.DeleteJobDbRecord(jobInfo.Id, js.Id);
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx, $"Failed to clean up job specification and submitted job info for job {jobInfo.Id} after directory creation failure.");
            }
            throw;
        }

        return jobInfo.ConvertIntToExt();
    }

    public async Task<SubmittedJobInfoExt> SubmitJobAsync(long createdJobInfoId, string sessionCode)
    {
        SubmittedJobInfo jobInfo;
        AdaptorUser loggedUser;
        bool isWaiting = false;

        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var job = await unitOfWork.SubmittedJobInfoRepository.GetByIdWithProjectAsync(createdJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", createdJobInfoId);
            loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, job.Project.Id, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            (jobInfo, isWaiting) = await jobLogic.PrepareJobForSubmitAsync(createdJobInfoId, loggedUser);
            if (isWaiting)
            {
                return jobInfo.ConvertIntToExt();
            }
        } // unitOfWork is disposed here!

        // SSH call: submit job
        var submittedTasks = await SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
            .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
            .SubmitJobAsync(jobInfo.Specification, jobInfo.Specification.ClusterUser, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        // Save submitted state to DB
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var updatedJob = await jobLogic.CompleteJobSubmitAsync(createdJobInfoId, loggedUser, submittedTasks);
            HEAppE.BusinessLogicTier.Logic.JobManagement.JobCacheManager.InvalidateJobCache(createdJobInfoId);
            return updatedJob.ConvertIntToExt();
        }
    }

    public async Task<SubmittedJobInfoExt> GetActualTasksInfo(long submittedJobInfoId, string sessionCode)
    {
        SubmittedJobInfo jobInfo;
        ClusterAuthenticationCredentials credentials;
        AdaptorUser loggedUser;

        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var job = await unitOfWork.SubmittedJobInfoRepository.GetByIdWithTasksAsync(submittedJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", submittedJobInfoId);
            loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, job.Project.Id, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            (jobInfo, credentials) = await jobLogic.PrepareGetActualTasksInfoAsync(submittedJobInfoId, loggedUser);
        } // unitOfWork disposed!

        // SSH call: get actual tasks info
        var cluster = jobInfo.Specification.Cluster;
        var actualUnfinishedSchedulerTasksInfo = await SchedulerFactory.GetInstance(cluster.SchedulerType)
            .CreateScheduler(cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
            .GetActualTasksInfoAsync(jobInfo.Tasks.Where(w => !w.Specification.DependsOn.Any()).ToList(), credentials, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        // Update DB
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var updatedJob = await jobLogic.CompleteGetActualTasksInfoAsync(submittedJobInfoId, loggedUser, actualUnfinishedSchedulerTasksInfo);
            return updatedJob.ConvertIntToExt();
        }
    }

    public async Task<SubmittedJobInfoExt> CancelJob(long submittedJobInfoId, string sessionCode)
    {
        SubmittedJobInfo jobInfo;
        ClusterAuthenticationCredentials credentials;
        AdaptorUser loggedUser;
        bool cancelledLocally = false;

        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var job = await unitOfWork.SubmittedJobInfoRepository.GetByIdWithTasksAsync(submittedJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", submittedJobInfoId);
            loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, job.Project.Id, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            (jobInfo, credentials, cancelledLocally) = await jobLogic.PrepareCancelJobAsync(submittedJobInfoId, loggedUser);
            if (cancelledLocally)
            {
                HEAppE.BusinessLogicTier.Logic.JobManagement.JobCacheManager.InvalidateJobCache(submittedJobInfoId);
                return jobInfo.ConvertIntToExt();
            }
        } // unitOfWork is disposed here!

        // SSH calls: Cancel and then GetActualTasks
        var scheduler = SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
            .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger);
        
        var submittedTask = jobInfo.Tasks.Where(w => !w.Specification.DependsOn.Any()).ToList();
        await scheduler.CancelJobAsync(submittedTask, "Job cancelled manually by the client.",
            jobInfo.Specification.ClusterUser, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        var actualUnfinishedSchedulerTasksInfo = await scheduler.GetActualTasksInfoAsync(submittedTask, credentials, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        // Update DB
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var updatedJob = await jobLogic.CompleteCancelJobAsync(submittedJobInfoId, loggedUser, actualUnfinishedSchedulerTasksInfo);
            HEAppE.BusinessLogicTier.Logic.JobManagement.JobCacheManager.InvalidateJobCache(submittedJobInfoId);
            return updatedJob.ConvertIntToExt();
        }
    }

    public async Task<bool> DeleteJob(long submittedJobInfoId, bool archiveLogs, string sessionCode)
    {
        SubmittedJobInfo jobInfo;
        ClusterProject clusterProject;
        AdaptorUser loggedUser;

        // 1. Prepare/Check delete and archive
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var job = await unitOfWork.SubmittedJobInfoRepository.GetByIdWithTasksAsync(submittedJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", submittedJobInfoId);
            loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, job.Project.Id, _expirioService);

            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            (jobInfo, clusterProject) = await jobLogic.PrepareDeleteJobAsync(submittedJobInfoId, loggedUser);
        } // unitOfWork disposed!

        // 2. Perform SSH archive if requested
        if (archiveLogs)
        {
            _logger.LogInformation($"Archiving job logs {submittedJobInfoId} by user {loggedUser.Id}");
            
            SubmittedJobInfo archJobInfo;
            IEnumerable<Tuple<string, string>> sourceDestinations;
            using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
            {
                var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
                (archJobInfo, _, _, sourceDestinations) = await jobLogic.PrepareArchiveJobAsync(submittedJobInfoId, loggedUser);
            }
            
            await SchedulerFactory.GetInstance(archJobInfo.Specification.Cluster.SchedulerType)
                .CreateScheduler(archJobInfo.Specification.Cluster, archJobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
                .MoveJobFilesAsync(archJobInfo, sourceDestinations, BusinessLogicConfiguration.SharedAccountsPoolMode, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
        }

        // 3. Perform SSH delete
        var isDeleted = await SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
            .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
            .DeleteJobDirectoryAsync(jobInfo, clusterProject.ScratchStoragePath, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        // 4. Update DB state on success
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var result = await jobLogic.CompleteDeleteJobAsync(submittedJobInfoId, loggedUser, isDeleted);
            HEAppE.BusinessLogicTier.Logic.JobManagement.JobCacheManager.InvalidateJobCache(submittedJobInfoId);
            return result;
        }
    }

    public async Task<SubmittedJobInfoExt[]> ListJobsForCurrentUser(
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
            .Include(x => x.Specification)
                .ThenInclude(s => s.SubProject)
            .Include(x => x.Specification)
                .ThenInclude(s => s.Cluster)
            .Include(x => x.Project)
                .ThenInclude(p => p.ClusterProjects)
                    .ThenInclude(cp => cp.Cluster)
            .Include(x => x.Tasks)
                .ThenInclude(t => t.NodeType)
            .Include(x => x.Tasks)
                .ThenInclude(t => t.Project)
            .Include(x => x.Tasks)
                .ThenInclude(t => t.TaskAllocationNodes)
            .Include(x => x.Tasks)
                .ThenInclude(t => t.Specification)
                    .ThenInclude(ts => ts.CommandTemplate)
                        .ThenInclude(ct => ct.TemplateParameters);
        
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

        var results = await query.ToListAsync();
        await unitOfWork.SubmittedJobInfoRepository.AttachCommandTemplatesIncludingDeletedAsync(results.SelectMany(r => r.Tasks));
        return results
            .Select(x => x.ConvertIntToExt())
            .ToArray();
    }

    public async Task<SubmittedJobInfoExt> CurrentInfoForJob(long submittedJobInfoId, string sessionCode)
    {
        // Serialize concurrent requests for the same job to avoid N parallel DB/SSH calls
        var semaphore = _jobSemaphores.GetOrAdd(submittedJobInfoId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            SubmittedJobInfo job;
            AdaptorUser loggedUser;
            bool isAdmin;
            bool isJobOwner;

            using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
            {
                job = await unitOfWork.SubmittedJobInfoRepository.GetByIdWithProjectAsync(submittedJobInfoId) ??
                          throw new InputValidationException("NotExistingJob", submittedJobInfoId);
                loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                    _logger, AdaptorUserRoleType.Submitter, job.Project.Id, _expirioService);

                long projectId = job.Project?.Id ?? 0;
                isAdmin = UserAndLimitationManagementService.CheckIfUserHasRoleForProject(loggedUser, AdaptorUserRoleType.Administrator, projectId, true);
                isJobOwner = job.Submitter.Id == loggedUser.Id;

                if (!isJobOwner && !isAdmin)
                {
                    throw new AdaptorUserNotAuthorizedForJobException("UserNotAuthorizedToWorkWithJob",
                        loggedUser.GetLogIdentification(), job.Id);
                }

                // Check cache using job-specific key
                string cacheKey = $"CurrentInfoForJob_{submittedJobInfoId}";
                if (_cache.TryGetValue(cacheKey, out SubmittedJobInfoExt cachedJobInfo))
                {
                    _logger.LogDebug("Returning cached job info for job {JobId}", submittedJobInfoId);
                    return cachedJobInfo;
                }

                var jobWithCluster = await unitOfWork.SubmittedJobInfoRepository.GetByIdWithTasksAsync(submittedJobInfoId);
                var cluster = jobWithCluster?.Specification?.Cluster;
                var clusterConfig = ClusterRuntimeConfiguration.For(cluster?.CustomConfiguration);
                bool callbackEnabled = clusterConfig.Scripts.UseCallbackForHpcJobs;

                bool needSshRefresh = !callbackEnabled
                                      && JwtTokenIntrospectionConfiguration.IsEnabled
                                      && SshCaSettings.UseCertificateAuthorityForAuthentication
                                      && isJobOwner
                                      && (job.State == JobState.Running || job.State == JobState.Queued);
                
                if (!needSshRefresh && !callbackEnabled)
                {
                    // solution for FirecREST
                    bool hasToken() => !string.IsNullOrEmpty(!string.IsNullOrEmpty(_httpContextKeys.Context.LEXISToken) ? _httpContextKeys.Context.LEXISToken : _httpContextKeys.Context.IdpToken);
                    if (hasToken())
                    {
                        var jobHelper = unitOfWork.SubmittedJobInfoRepository.GetQueryableWithoutFilters()
                            .Include(j => j.Project)
                                .ThenInclude(p => p.ClusterProjects)
                                    .ThenInclude(cp => cp.Cluster)
                            .Where(j => j.Id == submittedJobInfoId).FirstOrDefault();
                        bool isFirecRestJob() => (job.State == JobState.Running || job.State == JobState.Queued || job.State == JobState.Submitted) && jobHelper.Project.ClusterProjects.Any(cp => !cp.Cluster.IsDeleted && (cp.Cluster.SchedulerType & SchedulerType.FirecRestSlurm) == SchedulerType.FirecRestSlurm);
                        if (isFirecRestJob())
                            needSshRefresh = true;
                    }
                }

                if (!needSshRefresh && !callbackEnabled)
                {
                    // Kerberos clusters (e.g. Metacentrum) have no background polling — status must
                    // be fetched on-demand via the scheduler, not served from stale DB state.
                    if (job.State == JobState.Running || job.State == JobState.Queued || job.State == JobState.Submitted)
                    {
                        var jobWithSpec = unitOfWork.SubmittedJobInfoRepository.GetQueryableWithoutFilters()
                            .Include(j => j.Specification)
                                .ThenInclude(s => s.ClusterUser)
                            .Where(j => j.Id == submittedJobInfoId)
                            .FirstOrDefault();
                        if (jobWithSpec?.Specification?.ClusterUser?.AuthenticationType == ClusterAuthenticationCredentialsAuthType.Kerberos)
                            needSshRefresh = true;
                    }
                }

                if (!needSshRefresh)
                {
                    // DB-only path: use lightweight query - no SSH navigation properties needed
                    var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
                    var jobInfo = jobLogic.GetSubmittedJobInfoByIdForStatus(submittedJobInfoId, loggedUser, isAdmin);
                    var result = jobInfo.ConvertIntToExt();
                    // Cache DB-only responses for 10s to absorb concurrent poll bursts
                    var cts = HEAppE.BusinessLogicTier.Logic.JobManagement.JobCacheManager.JobCacheTokens.GetOrAdd(submittedJobInfoId, _ => new System.Threading.CancellationTokenSource());
                    var cacheEntryOptions = new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromSeconds(10))
                        .AddExpirationToken(new Microsoft.Extensions.Primitives.CancellationChangeToken(cts.Token));
                    _cache.Set(cacheKey, result, cacheEntryOptions);
                    return result;
                }
            } // unitOfWork disposed - DB connection released before SSH call

            // SSH path: job is Running/Queued under introspection mode.
            // NOTE: SSH/HPC scheduler responses are NOT cached — external API results must not be cached.
            // Concurrent requests for the same job are already serialized by the semaphore above.
            var sshResult = await GetActualTasksInfo(submittedJobInfoId, sessionCode);
            return sshResult;
        }
        finally
        {
            semaphore.Release();
            // Remove the semaphore from the dictionary once no other thread is waiting on it.
            // CurrentCount == 1 means the semaphore is now free (no concurrent waiter).
            // Small intentional race: if another thread calls GetOrAdd between our check and
            // TryRemove, it will simply re-add a fresh semaphore — safe and correct.
            if (semaphore.CurrentCount == 1)
            {
                _jobSemaphores.TryRemove(submittedJobInfoId, out _);
            }
        }
    }

    public async Task CopyJobDataToTempAsync(long createdJobInfoId, string sessionCode, string path)
    {
        SubmittedJobInfo jobInfo;
        ClusterProject clusterProject;
        AdaptorUser loggedUser;

        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetByIdWithProject(createdJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", createdJobInfoId);
            loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, job.Project.Id, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            (jobInfo, clusterProject) = await jobLogic.PrepareCopyJobDataToTempAsync(createdJobInfoId, loggedUser);
        } // unitOfWork is disposed here!

        await SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
            .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
            .CopyJobDataToTempAsync(jobInfo, clusterProject.ScratchStoragePath, sessionCode, path, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
    }

    public async Task CopyJobDataFromTempAsync(long createdJobInfoId, string sessionCode, string tempSessionCode)
    {
        SubmittedJobInfo jobInfo;
        ClusterProject clusterProject;
        AdaptorUser loggedUser;

        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetByIdWithProject(createdJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", createdJobInfoId);
            loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, job.Project.Id, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            (jobInfo, clusterProject) = await jobLogic.PrepareCopyJobDataFromTempAsync(createdJobInfoId, loggedUser);
        } // unitOfWork is disposed here!

        await SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
            .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
            .CopyJobDataFromTempAsync(jobInfo, clusterProject.ScratchStoragePath, tempSessionCode, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
    }

    public async Task<IEnumerable<string>> AllocatedNodesIPsAsync(long submittedTaskInfoId, string sessionCode)
    {
        SubmittedTaskInfo taskInfo;
        AdaptorUser loggedUser;

        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var task = unitOfWork.SubmittedTaskInfoRepository.GetByIdWithProject(submittedTaskInfoId);
            if (task is null) throw new InputValidationException("NotExistingTask", submittedTaskInfoId);
            loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, task.Project.Id, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            taskInfo = await jobLogic.PrepareGetAllocatedNodesIPsAsync(submittedTaskInfoId, loggedUser);
        } // unitOfWork is disposed!

        var cluster = taskInfo.Specification.JobSpecification.Cluster;
        var stringIPs = await SchedulerFactory.GetInstance(cluster.SchedulerType)
            .CreateScheduler(cluster, taskInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
            .GetAllocatedNodesAsync(taskInfo, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        return stringIPs.ToArray();
    }

    public async Task<DryRunJobInfoExt> DryRunJob(long modelProjectId, long modelClusterNodeTypeId, long modelNodes,
        long modelTasksPerNode,
        long modelWallTimeInMinutes, string modelSessionCode)
    {
        DryRunJobSpecification dryRunJobSpecification;
        Cluster cluster;
        Project project;
        AdaptorUser loggedUser;

        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, modelProjectId, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            (dryRunJobSpecification, cluster, project) = await jobLogic.PrepareDryRunJobAsync(modelProjectId, modelClusterNodeTypeId, modelNodes,
                modelTasksPerNode, modelWallTimeInMinutes, loggedUser);
        } // unitOfWork is disposed!

        var dryRunResult = await SchedulerFactory.GetInstance(cluster.SchedulerType)
            .CreateScheduler(cluster, project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
            .DryRunJobAsync(dryRunJobSpecification, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        return dryRunResult.ConvertIntToExt();
    }

    public async Task ProcessTaskCallbackAsync(string scheduledJobId, string token, string? rawResponse, string? qSchedulerState)
    {
        long jobId;
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            jobId = await jobLogic.ProcessTaskCallbackAsync(scheduledJobId, token, rawResponse, qSchedulerState);
        }

        // Directly remove the cache entry so the next poll sees the updated state immediately.
        // CancellationChangeToken eviction is lazy/async — we need synchronous removal here.
        _cache.Remove($"CurrentInfoForJob_{jobId}");
        HEAppE.BusinessLogicTier.Logic.JobManagement.JobCacheManager.InvalidateJobCache(jobId);
    }

    public async Task<long> OpenQSchedulerSessionAsync(long clusterId, long projectId, string machineId, int walltimeLimitSecs, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, projectId, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            return await jobLogic.OpenQSchedulerSessionAsync(clusterId, projectId, machineId, walltimeLimitSecs, loggedUser);
        }
    }

    public async Task CloseQSchedulerSessionAsync(long clusterId, long projectId, long sessionId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, projectId, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            await jobLogic.CloseQSchedulerSessionAsync(clusterId, projectId, sessionId, loggedUser);
        }
    }

    public async Task<QSchedulerSessionInfoExt> GetQSchedulerSessionInfoAsync(long sessionId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var (loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, _expirioService);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var session = await jobLogic.GetQSchedulerSessionInfoAsync(sessionId, loggedUser);
            return new QSchedulerSessionInfoExt
            {
                SessionId = session.SessionId,
                ClusterId = session.ClusterId,
                ProjectId = session.ProjectId,
                State = session.State.ToString(),
                CreatedAt = session.CreatedAt,
                ClosedAt = session.ClosedAt
            };
        }
    }

    public async Task<SubmittedJobInfoExt> CreateAndSubmitQSchedulerJob(QSchedulerJobSpecificationExt specification, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, specification.ProjectId, _expirioService);

            // Auto-resolve NodeType for the specified cluster
            var nodeTypes = await unitOfWork.ClusterNodeTypeRepository.GetAllWithPossibleCommandsAsync();
            var nodeType = nodeTypes.FirstOrDefault(n => n.ClusterId == specification.ClusterId)
                ?? throw new Exceptions.External.InvalidRequestException("NoNodeTypesConfigured");

            long? transferMethodId = nodeType.FileTransferMethodId;
            if (!transferMethodId.HasValue)
            {
                var transferMethods = await unitOfWork.FileTransferMethodRepository.GetAllAsync();
                var transferMethod = transferMethods.FirstOrDefault()
                    ?? throw new Exceptions.External.InvalidRequestException("NoTransferMethodsConfigured");
                transferMethodId = transferMethod.Id;
            }

            // Map target specification
            var tasks = new List<TaskSpecificationExt>();
            for (int i = 0; i < specification.Tasks.Length; i++)
            {
                var qTask = specification.Tasks[i];
                var envVars = new List<EnvironmentVariableExt>();

                // Add session variables internally if needed
                if (qTask.SessionId.HasValue)
                {
                    envVars.Add(new EnvironmentVariableExt { Name = "HEAPPE_QSCHEDULER_SESSION_ID", Value = qTask.SessionId.ToString() });
                    envVars.Add(new EnvironmentVariableExt { Name = "HEAPPE_QSCHEDULER_USE_SESSIONS", Value = "true" });
                }
                else if (qTask.UseSessions == true)
                {
                    envVars.Add(new EnvironmentVariableExt { Name = "HEAPPE_QSCHEDULER_USE_SESSIONS", Value = "true" });
                }

                // If PayloadFilePath is specified (relative path to job/task dir)
                string standardInputFile = "payload.json"; // default name
                if (!string.IsNullOrEmpty(qTask.PayloadFilePath))
                {
                    standardInputFile = qTask.PayloadFilePath;
                }

                tasks.Add(new TaskSpecificationExt
                {
                    Name = qTask.Name,
                    MinCores = 1,
                    MaxCores = 1,
                    WalltimeLimit = qTask.WalltimeLimitSecs,
                    ClusterNodeTypeId = nodeType.Id,
                    CommandTemplateId = null,
                    EnvironmentVariables = envVars.ToArray(),
                    StandardInputFile = standardInputFile,
                    StandardOutputFile = "stdout.log",
                    StandardErrorFile = "stderr.log",
                    ProgressFile = "progress.log", // dummy but required by BLL validator
                    LogFile = "output.log"
                });
            }

            var jobSpec = new JobSpecificationExt
            {
                Name = specification.Name,
                ProjectId = specification.ProjectId,
                ClusterId = specification.ClusterId,
                FileTransferMethodId = transferMethodId,
                Tasks = tasks.ToArray()
            };

            // Call existing CreateJob implementation
            return await CreateJob(jobSpec, sessionCode);
        }
    }

    public async Task<IEnumerable<QSchedulerSessionInfoExt>> ListQSchedulerSessionsAsync(string sessionCode, string state = null, long? clusterId = null, long? projectId = null)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var (loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, _expirioService);

            QSchedulerSessionState? parsedState = null;
            if (!string.IsNullOrEmpty(state))
            {
                if (Enum.TryParse<QSchedulerSessionState>(state, true, out var result))
                {
                    parsedState = result;
                }
                else
                {
                    throw new Exceptions.External.InvalidRequestException("InvalidSessionStateFilter");
                }
            }

            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var sessions = await jobLogic.ListQSchedulerSessionsAsync(loggedUser, parsedState, clusterId, projectId);

            return sessions.Select(s => new QSchedulerSessionInfoExt
            {
                SessionId = s.SessionId,
                ClusterId = s.ClusterId,
                ProjectId = s.ProjectId,
                State = s.State.ToString(),
                CreatedAt = s.CreatedAt,
                ClosedAt = s.ClosedAt
            });
        }
    }

#pragma warning disable IDE1006
    private string _expirioToken
    {
        get => !string.IsNullOrEmpty(_httpContextKeys.Context.LEXISToken) ? _httpContextKeys.Context.LEXISToken : _httpContextKeys.Context.IdpToken;
    }
#pragma warning restore IDE1006

    #endregion
}