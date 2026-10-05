using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Options;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.ClusterInformation;
using HEAppE.BusinessLogicTier.Logic.JobManagement.Validators;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.Comparers;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Internal;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExternalAuthentication.DTO.LexisAuth;
using HEAppE.FileTransferFramework;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using HEAppE.Utils;
using HEAppE.DataAccessTier.Vault;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement;

public static class JobCacheManager
{
    public static readonly System.Collections.Concurrent.ConcurrentDictionary<long, System.Threading.CancellationTokenSource> JobCacheTokens = new();

    public static void InvalidateJobCache(long jobId)
    {
        if (JobCacheTokens.TryRemove(jobId, out var cts))
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch (Exception)
            {
                // Ignore
            }
        }
    }
}

internal class JobManagementLogic : IJobManagementLogic
{
    private readonly ILogger _logger;
    private readonly Dictionary<TaskSpecification, TaskSpecification> _extraLongTaskDecomposedDependency;
    private readonly object _lockCreateJobObj = new();
    private readonly object _lockSubmitJobObj = new();
    private readonly List<TaskSpecification> _tasksToAddToSpec;
    private readonly List<TaskSpecification> _tasksToDeleteFromSpec;
    protected IUnitOfWork _unitOfWork;
    protected ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IUserOrgService _userOrgService;
    private readonly IExpirioService _expirioService;
    private readonly Dictionary<SchedulerType, Callbacks.ISchedulerCallbackHandler> _callbackHandlers;

    internal ISshCertificateAuthorityService SshCertificateAuthorityService => _sshCertificateAuthorityService;
    internal IExpirioService ExpirioService => _expirioService;
    internal string ExpirioToken => _expirioToken;
    internal IHttpContextKeys HttpContextKeys => _httpContextKeys;

    internal JobManagementLogic(IUnitOfWork unitOfWork, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, 
                                IHttpContextKeys httpContextKeys, IExpirioService expirioService, ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _tasksToDeleteFromSpec = new List<TaskSpecification>();
        _tasksToAddToSpec = new List<TaskSpecification>();
        _extraLongTaskDecomposedDependency = new Dictionary<TaskSpecification, TaskSpecification>();
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _userOrgService = userOrgService;
        _logger = logger;
        _expirioService = expirioService;

        _callbackHandlers = new Dictionary<SchedulerType, Callbacks.ISchedulerCallbackHandler>
        {
            { SchedulerType.QScheduler, new Callbacks.QSchedulerCallbackHandler(unitOfWork, logger, this) },
            { SchedulerType.Slurm, new Callbacks.DefaultCallbackHandler(this, logger) },
            { SchedulerType.PbsPro, new Callbacks.DefaultCallbackHandler(this, logger) },
            { SchedulerType.LinuxLocal, new Callbacks.DefaultCallbackHandler(this, logger) },
            { SchedulerType.HyperQueue, new Callbacks.DefaultCallbackHandler(this, logger) },
            { SchedulerType.FirecRestSlurm, new Callbacks.DefaultCallbackHandler(this, logger) }
        };
    }


    public async Task<SubmittedJobInfo> CreateJob(JobSpecification specification, AdaptorUser loggedUser,
        bool isExtraLong)
    {
        var jobInfo = await CreateJobDbRecord(specification, loggedUser, isExtraLong);
        var clusterProject =
            _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(
                jobInfo.Specification.ClusterId, jobInfo.Project.Id)
            ?? throw new InvalidRequestException("NotExistingProject");

        try
        {
            //Create job directory
            await SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
                .CreateScheduler(specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService,
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
                await DeleteJobDbRecord(jobInfo.Id, specification.Id);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx, $"Failed to clean up job specification and submitted job info for job {jobInfo.Id} after directory creation failure.");
            }
            throw;
        }

        return jobInfo;
    }

    public virtual async Task<SubmittedJobInfo> SubmitJobAsync(long createdJobInfoId, AdaptorUser loggedUser)
    {
        _logger.LogInformation($"User {loggedUser.GetLogIdentification()} is submitting the job with info Id {createdJobInfoId}");
        var (jobInfo, isWaiting) = await PrepareJobForSubmitAsync(createdJobInfoId, loggedUser);
        if (isWaiting)
        {
            return jobInfo;
        }

        var submittedTasks = await SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
            .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
            .SubmitJobAsync(jobInfo.Specification, jobInfo.Specification.ClusterUser, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        return await CompleteJobSubmitAsync(createdJobInfoId, loggedUser, submittedTasks);
    }

    public async Task<SubmittedJobInfo> GetActualTasksInfo(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        _logger.LogInformation($"User {loggedUser.GetLogIdentification()} is getting actual tasks info for the job with info Id {submittedJobInfoId}");
        
        var jobInfo = await GetSubmittedJobInfoByIdAsync(submittedJobInfoId, loggedUser);
        var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        
        if (clusterConfig.EnableCallback)
        {
            _logger.LogInformation($"Callback is configured. Bypassing active cluster check for job {submittedJobInfoId} and returning database state.");
            return jobInfo;
        }

        var (preparedJobInfo, credentials) = await PrepareGetActualTasksInfoAsync(submittedJobInfoId, loggedUser);
        
        var actualUnfinishedSchedulerTasksInfo = await SchedulerFactory.GetInstance(preparedJobInfo.Specification.Cluster.SchedulerType)
            .CreateScheduler(preparedJobInfo.Specification.Cluster, preparedJobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
            .GetActualTasksInfoAsync(preparedJobInfo.Tasks.Where(w => !w.Specification.DependsOn.Any()).ToList(), credentials, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        return await CompleteGetActualTasksInfoAsync(submittedJobInfoId, loggedUser, actualUnfinishedSchedulerTasksInfo);
    }

    public virtual async Task<SubmittedJobInfo> CancelJob(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        _logger.LogInformation(
            $"User {loggedUser.GetLogIdentification()} is canceling the job with info Id {submittedJobInfoId}");
        var (jobInfo, credentials, cancelledLocally) = await PrepareCancelJobAsync(submittedJobInfoId, loggedUser);
        if (cancelledLocally)
        {
            return jobInfo;
        }

        var scheduler = SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
            .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger);
        var submittedTask = jobInfo.Tasks.Where(w => !w.Specification.DependsOn.Any()).ToList();
        await scheduler.CancelJobAsync(submittedTask, "Job cancelled manually by the client.",
            jobInfo.Specification.ClusterUser, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        var actualUnfinishedSchedulerTasksInfo = await scheduler.GetActualTasksInfoAsync(submittedTask, credentials, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        return await CompleteCancelJobAsync(submittedJobInfoId, loggedUser, actualUnfinishedSchedulerTasksInfo);
    }

    public virtual async Task<bool> DeleteJobAsync(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        _logger.LogInformation($"User {loggedUser.GetLogIdentification()} is deleting the job with info Id {submittedJobInfoId}");
        var (jobInfo, clusterProject) = await PrepareDeleteJobAsync(submittedJobInfoId, loggedUser);

        var isDeleted = await SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
            .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
            .DeleteJobDirectoryAsync(jobInfo, clusterProject.ScratchStoragePath, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        return await CompleteDeleteJobAsync(submittedJobInfoId, loggedUser, isDeleted);
    }
    
    public virtual async Task<bool> ArchiveJobAsync(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        _logger.LogInformation($"User {loggedUser.GetLogIdentification()} is archiving the job with info Id {submittedJobInfoId}");
        var (jobInfo, _, _, sourceDestinations) = await PrepareArchiveJobAsync(submittedJobInfoId, loggedUser);

        var isArchived = await SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType).
            CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger).
            MoveJobFilesAsync(jobInfo, sourceDestinations, BusinessLogicConfiguration.SharedAccountsPoolMode, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
        return isArchived;
    }

    public SubmittedJobInfo GetSubmittedJobInfoById(long submittedJobInfoId, AdaptorUser loggedUser, bool isAdminOverride = false)
    {
        var jobInfo = _unitOfWork.SubmittedJobInfoRepository.GetByIdWithTasks(submittedJobInfoId)
                      ?? throw new RequestedObjectDoesNotExistException("NotExistingJobInfo", submittedJobInfoId);

        if (!LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
                 .AuthorizeUserForJobInfo(loggedUser, jobInfo, isAdminOverride))
            throw new AdaptorUserNotAuthorizedForJobException("UserNotAuthorizedToWorkWithJob",
                loggedUser.GetLogIdentification(), submittedJobInfoId);
        return jobInfo;
    }

    public async Task<SubmittedJobInfo> GetSubmittedJobInfoByIdAsync(long submittedJobInfoId, AdaptorUser loggedUser, bool isAdminOverride = false)
    {
        var jobInfo = await _unitOfWork.SubmittedJobInfoRepository.GetByIdWithTasksAsync(submittedJobInfoId)
                      ?? throw new RequestedObjectDoesNotExistException("NotExistingJobInfo", submittedJobInfoId);

        if (!LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
                 .AuthorizeUserForJobInfo(loggedUser, jobInfo, isAdminOverride))
            throw new AdaptorUserNotAuthorizedForJobException("UserNotAuthorizedToWorkWithJob",
                loggedUser.GetLogIdentification(), submittedJobInfoId);
        return jobInfo;
    }

    /// <summary>
    /// Lightweight status read using minimal DB query (no SSH navigation properties).
    /// Use for read-only status polling when SSH is not needed.
    /// </summary>
    public SubmittedJobInfo GetSubmittedJobInfoByIdForStatus(long submittedJobInfoId, AdaptorUser loggedUser, bool isAdminOverride = false)
    {
        var jobInfo = _unitOfWork.SubmittedJobInfoRepository.GetByIdForStatus(submittedJobInfoId)
                      ?? throw new RequestedObjectDoesNotExistException("NotExistingJobInfo", submittedJobInfoId);

        if (!LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
                 .AuthorizeUserForJobInfo(loggedUser, jobInfo, isAdminOverride))
            throw new AdaptorUserNotAuthorizedForJobException("UserNotAuthorizedToWorkWithJob",
                loggedUser.GetLogIdentification(), submittedJobInfoId);
        return jobInfo;
    }

    public async Task<SubmittedJobInfo> GetSubmittedJobInfoByIdForStatusAsync(long submittedJobInfoId, AdaptorUser loggedUser, bool isAdminOverride = false)
    {
        var jobInfo = await _unitOfWork.SubmittedJobInfoRepository.GetByIdForStatusAsync(submittedJobInfoId)
                      ?? throw new RequestedObjectDoesNotExistException("NotExistingJobInfo", submittedJobInfoId);

        if (!LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
                 .AuthorizeUserForJobInfo(loggedUser, jobInfo, isAdminOverride))
            throw new AdaptorUserNotAuthorizedForJobException("UserNotAuthorizedToWorkWithJob",
                loggedUser.GetLogIdentification(), submittedJobInfoId);
        return jobInfo;
    }

    public async Task<SubmittedJobInfo> GetSubmittedJobInfoByIdForSubmitAsync(long submittedJobInfoId, AdaptorUser loggedUser, bool isAdminOverride = false)
    {
        var jobInfo = await _unitOfWork.SubmittedJobInfoRepository.GetByIdForSubmitAsync(submittedJobInfoId)
                      ?? throw new RequestedObjectDoesNotExistException("NotExistingJobInfo", submittedJobInfoId);

        if (!LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
                 .AuthorizeUserForJobInfo(loggedUser, jobInfo, isAdminOverride))
            throw new AdaptorUserNotAuthorizedForJobException("UserNotAuthorizedToWorkWithJob",
                loggedUser.GetLogIdentification(), submittedJobInfoId);
        return jobInfo;
    }

    static Tuple<string, string> CreatePathTuple(string localBasePath, string jobLogArchivePath, TaskSpecification task, string fileName)
    {
        var localPath = Path.Join(localBasePath,
            task.JobSpecification.Id.ToString(),
            task.Id.ToString(),
            string.IsNullOrEmpty(task.ClusterTaskSubdirectory) ? string.Empty : task.ClusterTaskSubdirectory,
            fileName);

        var archivePath = Path.Join(jobLogArchivePath,
            task.JobSpecification.Id.ToString(),
            task.Id.ToString(),
            string.IsNullOrEmpty(task.ClusterTaskSubdirectory) ? string.Empty : task.ClusterTaskSubdirectory,
            fileName);

        return new Tuple<string, string>(localPath, archivePath);
    }

    public virtual SubmittedTaskInfo GetSubmittedTaskInfoById(long submittedTaskInfoId, AdaptorUser loggedUser,
        bool checkSharedJobInfoAccess)
    {
        var taskInfo = _unitOfWork.SubmittedTaskInfoRepository.GetByIdWithJobSpecification(submittedTaskInfoId)
                      ?? throw new RequestedObjectDoesNotExistException("NotExistingTaskInfo", submittedTaskInfoId);

      if (!LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
                .AuthorizeUserForTaskInfo(loggedUser, taskInfo, checkSharedJobInfoAccess))
            throw new AdaptorUserNotAuthorizedForJobException("UserNotAuthorizedToWorkWithJob",
                loggedUser.GetLogIdentification(), submittedTaskInfoId);
        return taskInfo;
    }

    public virtual async Task<SubmittedTaskInfo> GetSubmittedTaskInfoByIdAsync(long submittedTaskInfoId, AdaptorUser loggedUser,
        bool checkSharedJobInfoAccess = false)
    {
        var taskInfo = await _unitOfWork.SubmittedTaskInfoRepository.GetByIdWithJobSpecificationAsync(submittedTaskInfoId)
                      ?? throw new RequestedObjectDoesNotExistException("NotExistingTaskInfo", submittedTaskInfoId);

      if (!LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
                .AuthorizeUserForTaskInfo(loggedUser, taskInfo, checkSharedJobInfoAccess))
            throw new AdaptorUserNotAuthorizedForJobException("UserNotAuthorizedToWorkWithJob",
                loggedUser.GetLogIdentification(), submittedTaskInfoId);
        return taskInfo;
    }

    public virtual SubmittedTaskInfo GetSubmittedTaskInfoById(long submittedTaskInfoId, AdaptorUser loggedUser)
    {
        var taskInfo = _unitOfWork.SubmittedTaskInfoRepository.GetByIdWithJobSpecification(submittedTaskInfoId)
                       ?? throw new RequestedObjectDoesNotExistException("NotExistingTaskInfo", submittedTaskInfoId);

        if (!LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
                .AuthorizeUserForTaskInfo(loggedUser, taskInfo))
            throw new AdaptorUserNotAuthorizedForJobException("UserNotAuthorizedToWorkWithTask",
                loggedUser.GetLogIdentification(), submittedTaskInfoId);
        return taskInfo;
    }

    public virtual IEnumerable<SubmittedJobInfo> GetJobsForUser(AdaptorUser loggedUser)
    {
        return _unitOfWork.SubmittedJobInfoRepository.GetAllForSubmitterId(loggedUser.Id);
    }

    public virtual async Task<IEnumerable<SubmittedJobInfo>> GetJobsForUserAsync(AdaptorUser loggedUser)
    {
        return await _unitOfWork.SubmittedJobInfoRepository.GetAllForSubmitterIdAsync(loggedUser.Id);
    }

    public virtual IEnumerable<SubmittedJobInfo> GetNotFinishedJobInfosForSubmitterId(long submitterId)
    {
        return _unitOfWork.SubmittedJobInfoRepository.GetNotFinishedForSubmitterId(submitterId);
    }

    public virtual async Task<IEnumerable<SubmittedJobInfo>> GetNotFinishedJobInfosForSubmitterIdAsync(long submitterId)
    {
        return await _unitOfWork.SubmittedJobInfoRepository.GetNotFinishedForSubmitterIdAsync(submitterId);
    }

    public virtual IEnumerable<SubmittedJobInfo> GetNotFinishedJobInfos()
    {
        return _unitOfWork.SubmittedJobInfoRepository.GetAllUnfinished();
    }

    public virtual async Task<IEnumerable<SubmittedJobInfo>> GetNotFinishedJobInfosAsync()
    {
        return await _unitOfWork.SubmittedJobInfoRepository.GetAllUnfinishedAsync();
    }

    public IEnumerable<SubmittedTaskInfo> GetAllFinishedTaskInfos(IEnumerable<long> taskIds)
    {
        if (taskIds == null || !taskIds.Any())
        {
            return Enumerable.Empty<SubmittedTaskInfo>();
        }
        return _unitOfWork.SubmittedTaskInfoRepository.GetFinishedByIds(taskIds);
    }

    public async Task<IEnumerable<SubmittedTaskInfo>> GetAllFinishedTaskInfosAsync(IEnumerable<long> taskIds)
    {
        if (taskIds == null || !taskIds.Any())
        {
            return Enumerable.Empty<SubmittedTaskInfo>();
        }
        return await _unitOfWork.SubmittedTaskInfoRepository.GetFinishedByIdsAsync(taskIds);
    }
    
    public async Task UpdateCurrentStateOfUnfinishedJobs()
    {
        var allUnfinishedJobs = (await _unitOfWork.SubmittedJobInfoRepository.GetAllUnfinishedAsync())
            .Where(j => (j.Specification.Cluster.SchedulerType & SchedulerType.FirecRestSlurm) != SchedulerType.FirecRestSlurm)
            // Kerberos clusters (e.g. Metacentrum) have no persistent service account — job state
            // is fetched on-demand via CurrentInfoForJob/GetActualTasksInfo, not by background polling.
            .Where(j => j.Specification.ClusterUser?.AuthenticationType != ClusterAuthenticationCredentialsAuthType.Kerberos)
            .ToList();

        var jobsToPoll = new List<SubmittedJobInfo>();
        bool dbStateUpdated = false;

        foreach (var job in allUnfinishedJobs)
        {
            var clusterConfig = ClusterRuntimeConfiguration.For(job.Specification.Cluster.CustomConfiguration);
            if (clusterConfig.EnableCallback)
            {
                if (job.SubmitTime.HasValue)
                {
                     var elapsedSeconds = DateTime.UtcNow.Subtract(job.SubmitTime.Value).TotalSeconds;
                     bool isNeedUpdateJobState = false;
                     foreach (var task in job.Tasks.Where(t => t.State is > TaskState.Configuring and < TaskState.Finished))
                     {
                         var walltimeLimit = task.Specification?.WalltimeLimit ?? 0;
                         if (walltimeLimit > 0)
                         {
                             bool isTimeout = false;
                             string errorMsg = "";

                             if (task.State == TaskState.Running && task.StartTime.HasValue)
                             {
                                 var runningSeconds = DateTime.UtcNow.Subtract(task.StartTime.Value).TotalSeconds;
                                 if (runningSeconds > walltimeLimit + 60) // walltime + 60s grace buffer
                                 {
                                     isTimeout = true;
                                     errorMsg = $"Task timed out (exceeded walltime limit of {walltimeLimit}s on compute node by more than 60s).";
                                 }
                             }
                             else if (elapsedSeconds > 3 * walltimeLimit)
                             {
                                 isTimeout = true;
                                 errorMsg = $"Task timed out (exceeded 3x walltime limit of {walltimeLimit}s from submission time without starting/completing).";
                             }

                             if (isTimeout)
                             {
                                 _logger.LogWarning($"HPC task {task.Id} (Job {job.Id}) timed out. {errorMsg}");
                                 task.State = TaskState.Failed;
                                 task.ErrorMessage = errorMsg;
                                 task.EndTime = DateTime.UtcNow;
                                 if (task.CallbackSecret != null)
                                 {
                                     task.CallbackSecret = null;
                                 }
                                 isNeedUpdateJobState = true;
                                 dbStateUpdated = true;
                             }
                         }
                     }
                    if (isNeedUpdateJobState)
                    {
                        UpdateJobStateByTasks(job);
                        _unitOfWork.SubmittedJobInfoRepository.Update(job);
                        JobCacheManager.InvalidateJobCache(job.Id);
                    }
                }
                continue; // Skip active polling for this job
            }

            jobsToPoll.Add(job);
        }

        if (dbStateUpdated)
        {
            await _unitOfWork.SaveAsync();
        }

        var serviceAccountsCache = new Dictionary<(long ClusterId, long ProjectId), ClusterAuthenticationCredentials>();
        foreach (var job in jobsToPoll)
        {
            var cluster = job.Specification.Cluster;
            if (cluster.UpdateJobStateByServiceAccount.Value && 
                !(cluster.SchedulerType == SchedulerType.QScheduler && 
                  (cluster.ConnectionProtocol == ClusterConnectionProtocol.Http || cluster.ConnectionProtocol == ClusterConnectionProtocol.Https)))
            {
                var key = (job.Specification.ClusterId, job.Specification.ProjectId);
                if (!serviceAccountsCache.ContainsKey(key))
                {
                    try
                    {
                        var account = await _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                            key.ClusterId, key.ProjectId, requireIsInitialized: true, adaptorUserId: null, logger: _logger);
                        if (account != null)
                        {
                            serviceAccountsCache[key] = account;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "No service account credentials initialized for cluster {ClusterId} project {ProjectId}. Background polling will fall back to cluster user.", key.ClusterId, key.ProjectId);
                    }
                }
            }
        }

        var jobTaskDataList = jobsToPoll.Select(job => new
        {
            Job = job,
            Project = job.Project, 
            UnfinishedTasks = job.Tasks
                .Where(w => w.State is > TaskState.Configuring and (<= TaskState.Running or TaskState.Canceled))
                .ToList()
        }).ToList();

        var hpcQueryGroups = jobTaskDataList
            .GroupBy(j => new
            {
                ClusterId = j.Job.Specification.ClusterId,
                ClusterUsername = j.Job.Specification.ClusterUser?.Username ?? string.Empty
            })
            .ToList();

        foreach (var group in hpcQueryGroups)
        {
            var itemsInGroup = group.ToList();
            var firstItem = itemsInGroup.First();

            var cluster = firstItem.Job.Specification.Cluster;
            var clusterUser = firstItem.Job.Specification.ClusterUser;

            var groupTasksResult = new List<SubmittedTaskInfo>();
            var tasksList = itemsInGroup.SelectMany(s => s.UnfinishedTasks).ToList();

            if (!tasksList.Any())
            {
                continue;
            }

            foreach (var item in itemsInGroup)
            {
                if (IsWaitingLimitExceeded(item.Job))
                {
                    var tasksToCancel = item.Job.Tasks.Where(w => !w.Specification.DependsOn.Any()).ToList();
                    if (tasksToCancel.Any())
                    {
                        _logger.LogWarning($"Job {item.Job.Id} exceeded waiting limit. Cancelling...");
                        try
                        {
                            var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType)
                                .CreateScheduler(cluster, item.Project, _sshCertificateAuthorityService, adaptorUserId: item.Job.Submitter.Id, _expirioService, _expirioToken, _logger);
                            await scheduler.CancelJobAsync(tasksToCancel, "Job cancelled automatically by exceeding waiting limit.", item.Job.Specification.ClusterUser, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to auto-cancel job {item.Job.Id} after exceeding wait limit.");
                        }
                    }
                }
            }

            ClusterAuthenticationCredentials account = clusterUser;
            var firstTask = tasksList.First();
            var spec = firstTask.Specification.JobSpecification;

            if (cluster.UpdateJobStateByServiceAccount.Value)
            {
                serviceAccountsCache.TryGetValue((spec.ClusterId, spec.ProjectId), out account);
            }

            if (account == null)
            {
                account = spec.ClusterUser;
            }

            try
            {
                var associatedProject = itemsInGroup.First(i => i.UnfinishedTasks.Contains(firstTask)).Project;
                
                var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType)
                    .CreateScheduler(cluster, associatedProject, _sshCertificateAuthorityService, cluster.UpdateJobStateByServiceAccount.Value ? null : spec.Submitter.Id, _expirioService, _expirioToken, _logger);
                
                var clusterConfig = ClusterRuntimeConfiguration.For(cluster.CustomConfiguration);
                bool callbackEnabled = clusterConfig.EnableCallback;
                var tasksToQuery = new List<SubmittedTaskInfo>();

                foreach (var task in tasksList)
                {
                    if (callbackEnabled)
                    {
                        // Bypass active polling: preserve current state and add directly to groupTasksResult
                        _logger.LogDebug($"Callback is configured for cluster {cluster.Name}. Bypassing active polling for task {task.ScheduledJobId}. State remains: {task.State}");
                        groupTasksResult.Add(new SubmittedTaskInfo
                        {
                            Id = task.Id,
                            ScheduledJobId = task.ScheduledJobId,
                            State = task.State,
                            ErrorMessage = task.ErrorMessage,
                            StartTime = task.StartTime,
                            EndTime = task.EndTime,
                            AllocatedTime = task.AllocatedTime,
                            AllParameters = task.AllParameters
                        });
                    }
                    else
                    {
                        tasksToQuery.Add(task);
                    }
                }

                if (tasksToQuery.Any())
                {
                    _logger.LogInformation($"Requesting state for a bulk batch of {tasksToQuery.Count} tasks on cluster {cluster.Name} using account {account?.Username}");
                    var states = await scheduler.GetActualTasksInfoAsync(tasksToQuery, account, null, null);
                    if (states != null)
                    {
                        groupTasksResult.AddRange(states);
                    }
                }

                var missingTasks = tasksList
                    .Where(t => !groupTasksResult.Any(g => g.ScheduledJobId == t.ScheduledJobId))
                    .ToList();

                if (missingTasks.Any())
                {
                    _logger.LogInformation($"Bulk checking history/accounting for {missingTasks.Count} missing tasks on cluster {cluster.Name}");
                    
                    var historicalStates = await scheduler.GetHistoricalTasksInfoAsync(missingTasks, account, null, null);
                    if (historicalStates != null)
                    {
                        groupTasksResult.AddRange(historicalStates);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to retrieve statuses for a batch of tasks on cluster {cluster.Name}");
            }

            foreach (var item in itemsInGroup)
            {
                var submittedJob = item.Job;
                try
                {
                    LoggingUtils.AddJobIdToLogThreadContext(submittedJob.Id);
                    if (submittedJob.Submitter != null)
                    {
                        LoggingUtils.AddUserPropertiesToLogThreadContext(submittedJob.Submitter.Id, submittedJob.Submitter.Username, submittedJob.Submitter.Email);
                    }

                    var previousTaskStates = submittedJob.Tasks.ToDictionary(t => t.Id, t => t.State);
                    var previousJobState = submittedJob.State;

                    bool isNeedUpdateJobState = false;
                    foreach (var submittedTask in submittedJob.Tasks)
                    {
                        // Skip tasks that were not queried (e.g. Finished, Failed, or Configuring)
                        if (!(submittedTask.State > TaskState.Configuring && (submittedTask.State <= TaskState.Running || submittedTask.State == TaskState.Canceled)))
                        {
                            continue;
                        }

                        // Callback timeout protection: If callback is enabled and the task is stuck,
                        // mark it as Failed if time since submission exceeds 3x the walltime limit.
                        var clusterConfig = ClusterRuntimeConfiguration.For(cluster.CustomConfiguration);
                        bool callbackEnabled = clusterConfig.EnableCallback;

                        if (callbackEnabled && submittedJob.SubmitTime.HasValue)
                        {
                            var elapsedSeconds = DateTime.UtcNow.Subtract(submittedJob.SubmitTime.Value).TotalSeconds;
                            var walltimeLimit = submittedTask.Specification.WalltimeLimit;
                            if (walltimeLimit > 0 && elapsedSeconds > 3 * walltimeLimit)
                            {
                                bool shouldQueryBeforeFailing = !item.Project.IsOneToOneMapping;

                                if (shouldQueryBeforeFailing)
                                {
                                    _logger.LogWarning($"{cluster.SchedulerType} task {submittedTask.Id} (Job {submittedJob.Id}) exceeded 3x walltime limit with callback enabled (project without 1:1 user mapping). Actively querying status before failing.");
                                    int retryCount = 3;
                                    int delayMs = 1000;
                                    SubmittedTaskInfo? updatedTask = null;
                                    bool querySucceeded = false;

                                    for (int i = 0; i < retryCount; i++)
                                    {
                                        try
                                        {
                                            var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType)
                                                .CreateScheduler(cluster, submittedJob.Project, _sshCertificateAuthorityService, cluster.UpdateJobStateByServiceAccount.Value ? null : submittedJob.Submitter?.Id, _expirioService, _expirioToken, _logger);
                                            
                                            submittedTask.ForceStatusQuery = true;
                                            var activeStates = await scheduler.GetActualTasksInfoAsync(new List<SubmittedTaskInfo> { submittedTask }, account, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
                                            updatedTask = activeStates?.FirstOrDefault();
                                            querySucceeded = true;
                                            break;
                                        }
                                        catch (Exception queryEx)
                                        {
                                            _logger.LogError(queryEx, $"Failed to query {cluster.SchedulerType} task {submittedTask.Id} status after 3x walltime timeout. Attempt {i + 1} of {retryCount}.");
                                            if (i == retryCount - 1)
                                            {
                                                break;
                                            }
                                            await Task.Delay(delayMs);
                                            delayMs *= 2;
                                        }
                                    }

                                    if (querySucceeded)
                                    {
                                        if (updatedTask != null && updatedTask.State >= TaskState.Finished)
                                        {
                                            _logger.LogInformation($"{cluster.SchedulerType} task {submittedTask.Id} status check succeeded and task finished/failed: state={updatedTask.State}. Updating state instead of forcing failure.");
                                            submittedTask.State = updatedTask.State;
                                            submittedTask.ErrorMessage = updatedTask.ErrorMessage ?? submittedTask.ErrorMessage;
                                            submittedTask.StartTime = updatedTask.StartTime ?? submittedTask.StartTime;
                                            submittedTask.EndTime = updatedTask.EndTime ?? submittedTask.EndTime;
                                            isNeedUpdateJobState = true;
                                            continue;
                                        }
                                        else if (updatedTask != null && updatedTask.State > TaskState.Submitted)
                                        {
                                            _logger.LogInformation($"{cluster.SchedulerType} task {submittedTask.Id} status check succeeded and task is in state={updatedTask.State}. Keeping task alive.");
                                            submittedTask.State = updatedTask.State;
                                            submittedTask.StartTime = updatedTask.StartTime ?? submittedTask.StartTime;
                                            isNeedUpdateJobState = true;
                                            continue;
                                        }
                                        else
                                        {
                                            _logger.LogWarning($"{cluster.SchedulerType} task {submittedTask.Id} status query returned state={updatedTask?.State} or null. Marking as Failed.");
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogError($"Failed to query {cluster.SchedulerType} task {submittedTask.Id} status after all {retryCount} attempts. Marking as Failed.");
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning($"{cluster.SchedulerType} task {submittedTask.Id} (Job {submittedJob.Id}) exceeded 3x walltime limit and is in a project with 1:1 user mapping. Marking as Failed immediately.");
                                }

                                submittedTask.State = TaskState.Failed;
                                submittedTask.ErrorMessage = "Task timed out (exceeded 3x walltime limit with callback enabled).";
                                isNeedUpdateJobState = true;
                                continue;
                            }
                        }
                       
                        var actualUnfinishedSchedulerTaskInfo = groupTasksResult.FirstOrDefault(w => w.ScheduledJobId == submittedTask.ScheduledJobId);
                        if (actualUnfinishedSchedulerTaskInfo is null)
                        {
                            if (submittedTask.State is > TaskState.Configuring and (<= TaskState.Running or TaskState.Canceled))
                            {
                                submittedTask.State = TaskState.Failed;
                                submittedTask.StateSource = JobStateSource.BackgroundPoll;
                                submittedTask.StateUpdatedAt = DateTime.UtcNow;
                                isNeedUpdateJobState = true;
                            }
                        }
                        else if (submittedTask.State != actualUnfinishedSchedulerTaskInfo.State || submittedTask.ScheduledJobId != actualUnfinishedSchedulerTaskInfo.ScheduledJobId)
                        {
                            if (submittedTask.ScheduledJobId.StartsWith("session:") && 
                                !submittedTask.ScheduledJobId.Contains(":task:") && 
                                actualUnfinishedSchedulerTaskInfo.ScheduledJobId.Contains(":task:"))
                            {
                                if (long.TryParse(submittedTask.ScheduledJobId.Substring("session:".Length), out var sessionId))
                                {
                                    var dbSession = await _unitOfWork.QSchedulerSessionRepository.GetBySessionIdAsync(sessionId);
                                    if (dbSession != null && dbSession.State != QSchedulerSessionState.Open)
                                    {
                                        dbSession.State = QSchedulerSessionState.Open;
                                        _logger.LogInformation($"UpdateCurrentStateOfUnfinishedJobs: Session {sessionId} detected as open during polling. Updating state in database.");
                                        
                                        await PublishEventAsync(dbSession.UserId, "org.heappe.session.state-changed", "/heappe/sessions", new
                                        {
                                            sessionId = $"session:{sessionId}",
                                            state = "Open"
                                        });
                                    }
                                }
                            }

                            CombineSubmittedTaskInfoFromCluster(submittedTask, actualUnfinishedSchedulerTaskInfo);
                            submittedTask.StateSource = JobStateSource.BackgroundPoll;
                            submittedTask.StateUpdatedAt = DateTime.UtcNow;
                            isNeedUpdateJobState = true;
                        }
                    }

                    var jobStateChanged = UpdateJobStateByTasks(submittedJob);
                    if (isNeedUpdateJobState || jobStateChanged)
                    {
                        submittedJob.StateSource = JobStateSource.BackgroundPoll;
                        submittedJob.StateUpdatedAt = DateTime.UtcNow;
                        _unitOfWork.SubmittedJobInfoRepository.Update(submittedJob);
                        JobCacheManager.InvalidateJobCache(submittedJob.Id);
                        await PublishStateChangesAsync(submittedJob, previousTaskStates, previousJobState);
                    }

                    await CheckAndCloseQSchedulerSessionsAsync(submittedJob);
                }
                finally
                {
                    LoggingUtils.RemoveJobIdFromLogThreadContext();
                    if (submittedJob.Submitter != null)
                    {
                        LoggingUtils.RemoveUserPropertiesFromLogThreadContext();
                    }
                }
            }
        }

        await _unitOfWork.SaveAsync();
    }

    public async Task CopyJobDataToTempAsync(long createdJobInfoId, AdaptorUser loggedUser, string hash, string path)
    {
        _logger.LogInformation(string.Format("User {0} with job Id {1} is copying job data to temp {2}",
            loggedUser.GetLogIdentification(), createdJobInfoId, hash));
        var (jobInfo, clusterProject) = await PrepareCopyJobDataToTempAsync(createdJobInfoId, loggedUser);

        await SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
            .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService,
                loggedUser.Id, _expirioService, _expirioToken, _logger)
            .CopyJobDataToTempAsync(jobInfo, clusterProject.ScratchStoragePath, hash, path, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
    }

    public async Task CopyJobDataFromTempAsync(long createdJobInfoId, AdaptorUser loggedUser, string hash)
    {
        _logger.LogInformation(string.Format("User {0} with job Id {1} is copying job data from temp {2}",
            loggedUser.GetLogIdentification(), createdJobInfoId, hash));
        var (jobInfo, clusterProject) = await PrepareCopyJobDataFromTempAsync(createdJobInfoId, loggedUser);

        await SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
            .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService,
                loggedUser.Id, _expirioService, _expirioToken, _logger)
            .CopyJobDataFromTempAsync(jobInfo, clusterProject.ScratchStoragePath, hash, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
    }

    public async Task<IEnumerable<string>> GetAllocatedNodesIPsAsync(long submittedTaskInfoId, AdaptorUser loggedUser)
    {
        var taskInfo = await PrepareGetAllocatedNodesIPsAsync(submittedTaskInfoId, loggedUser);

        var cluster = taskInfo.Specification.JobSpecification.Cluster;
        var stringIPs = await SchedulerFactory.GetInstance(cluster.SchedulerType)
            .CreateScheduler(cluster, taskInfo.Project, _sshCertificateAuthorityService,
                loggedUser.Id, _expirioService, _expirioToken, _logger)
            .GetAllocatedNodesAsync(taskInfo, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
        return stringIPs;
    }

    public async Task<DryRunJobInfo> DryRunJob(long modelProjectId, long modelClusterNodeTypeId, long modelNodes,
        long modelTasksPerNode,
        long modelWallTimeInMinutes, AdaptorUser loggedUser)
    {
        var (dryRunJobSpecification, cluster, project) = await PrepareDryRunJobAsync(modelProjectId, modelClusterNodeTypeId, modelNodes, modelTasksPerNode, modelWallTimeInMinutes, loggedUser);
        return await SchedulerFactory.GetInstance(cluster.SchedulerType)
            .CreateScheduler(cluster, project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
            .DryRunJobAsync(dryRunJobSpecification, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
    }

    public IQueryable<SubmittedJobInfo> GetJobsForUserQuery(long loggedUserId)
    {
        return _unitOfWork.SubmittedJobInfoRepository.GetJobsForUserQuery(loggedUserId);
    }

    protected void CompleteJobSpecification(JobSpecification specification, AdaptorUser loggedUser,
        IClusterInformationLogic clusterLogic, IUserAndLimitationManagementLogic userLogic, ClusterAuthenticationCredentials credentials)
    {
        try
        {
            var cluster = clusterLogic.GetClusterById(specification.ClusterId);
            specification.Cluster = cluster;

            specification.FileTransferMethod = LogicFactory.GetLogicFactory().CreateFileTransferLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
            .GetFileTransferMethodsByClusterId(cluster.Id)
            .FirstOrDefault(f => f.Id == specification.FileTransferMethodId.Value);

            specification.ClusterUser = credentials;
            specification.Submitter = _unitOfWork.AdaptorUserRepository.GetById(loggedUser.Id);
            var defaultGroup = specification.SubmitterGroup ??= userLogic.GetDefaultSubmitterGroup(specification.Submitter, specification.ProjectId);
            if (defaultGroup != null)
            {
                specification.SubmitterGroup = _unitOfWork.AdaptorUserGroupRepository.GetById(defaultGroup.Id);
            }
            specification.Project = _unitOfWork.ProjectRepository.GetById(specification.ProjectId);
            if (specification.SubProjectId.HasValue)
                specification.SubProject = _unitOfWork.SubProjectRepository.GetById(specification.SubProjectId.Value);
        } catch (Exception ex)
        {
            _logger.LogError(ex, "A problem occured during preparation of job specification values.");
            throw;
        }

        foreach (var task in specification.Tasks)
        {
            try
            {
                if (task.CommandTemplateId.HasValue)
                {
                    var commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(task.CommandTemplateId.Value);
                    if (commandTemplate != null && commandTemplate.IsGeneric)
                    {
                        //dynamically get parameters and their values and parse user-defined parameters to new parameter [name at db]
                        //if you want to, refactoring is possible
                        if (task.CommandParameterValues == null)
                        {
                            throw new InputValidationException("NotValidJobSpecification", "CommandParameterValues cannot be null.");
                        }

                        if (commandTemplate.TemplateParameters == null || !commandTemplate.TemplateParameters.Any())
                    {
                        throw new InputValidationException("NotValidJobSpecification", "Template parameters are not defined for the generic command template.");
                    }

                    var definedGenericCommandParameters = commandTemplate.TemplateParameters
                        .Select(x => x.Identifier)
                        .ToList();
                    var userDefinedCommandParameters = task.CommandParameterValues
                        .Where(x => !definedGenericCommandParameters.Contains(x.CommandParameterIdentifier))
                        .ToList();
                    var userScriptParameter = task.CommandParameterValues
                        .FirstOrDefault(x => definedGenericCommandParameters.Contains(x.CommandParameterIdentifier));

                    if (userScriptParameter == null)
                    {
                        throw new InputValidationException("NotValidJobSpecification", "User script path parameter, for generic command template, does not have a value.");
                    }

                    var userParametersParameter = commandTemplate.TemplateParameters
                        .FirstOrDefault(x => x.Identifier != userScriptParameter.CommandParameterIdentifier);

                    if (userParametersParameter == null)
                    {
                        throw new InputValidationException("NotValidJobSpecification", "User parameters parameter is not defined for the generic command template.");
                    }

                    var userParametersParameterName = userParametersParameter.Identifier;
                    var parsedUserParameter = AddGenericCommandUserDefinedCommands(userDefinedCommandParameters);

                    task.CommandParameterValues.Add(new CommandTemplateParameterValue
                    {
                        CommandParameterIdentifier = userParametersParameterName,
                        Value = parsedUserParameter //validate if value does not contain some prohibited parameters
                    });
                    task.CommandParameterValues.RemoveAll(x => userDefinedCommandParameters.Contains(x));
                }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A problem occured during processing of taks's command template.");
                throw;
            }

            try
            {
                CompleteTaskSpecification(task, clusterLogic);
                task.EnvironmentVariables =
                    CombineJobAndTaskEnvironmentVariables(specification.EnvironmentVariables, task.EnvironmentVariables)
                        .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception during completing task specification.");
                throw;
            }
        }
    }

    private string AddGenericCommandUserDefinedCommands(List<CommandTemplateParameterValue> templateParameters)
    {
        if (templateParameters.Count == 0) return string.Empty;

        var commandParametersSb = new StringBuilder(" \"");

        for (var i = 0; i < templateParameters.Count; i++)
        {
            var parameter = templateParameters[i];
            if (parameter.Value.Contains("\"")) //todo move to validator?
                throw new InvalidRequestException("ParameterIllegalCharacters", parameter.CommandParameterIdentifier,
                    parameter.Value);
            var parameterPair = $"{parameter.CommandParameterIdentifier}=\\\"{parameter.Value}\\\"";
            commandParametersSb.Append(parameterPair);

            if (i < templateParameters.Count - 1) commandParametersSb.Append(' ');
        }

        commandParametersSb.Append("\"");
        return commandParametersSb.ToString();
    }

    protected void CompleteTaskSpecification(TaskSpecification taskSpecification, IClusterInformationLogic clusterLogic)
    {
        taskSpecification.ClusterNodeType = clusterLogic.GetClusterNodeTypeById(taskSpecification.ClusterNodeTypeId);
        
        if (taskSpecification.CommandTemplateId.HasValue)
        {
            taskSpecification.CommandTemplate =
                _unitOfWork.CommandTemplateRepository.GetById(taskSpecification.CommandTemplateId.Value);
        }

        if (taskSpecification.CommandParameterValues?.Any() == true && taskSpecification.CommandTemplate?.TemplateParameters != null)
        {
            // Map parameters directly from the in-memory eager-loaded collection of the CommandTemplate.
            // This avoids an extra database query and resolves change tracking conflicts by using the same tracked instances.
            var paramLookup = taskSpecification.CommandTemplate.TemplateParameters
                .ToDictionary(p => p.Identifier, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var cmdParameterValue in taskSpecification.CommandParameterValues)
            {
                paramLookup.TryGetValue(cmdParameterValue.CommandParameterIdentifier, out var param);
                cmdParameterValue.TemplateParameter = param;
            }
        }

        //Combination parameters from template
        taskSpecification.Priority ??= default;

        taskSpecification.Project ??= taskSpecification.JobSpecification.Project;
    }

    /// <summary>
    ///     Divide extra long task to smaller tasks
    /// </summary>
    /// <param name="task">Task to divide</param>
    protected void DecomposeExtraLongTask(TaskSpecification task)
    {
        if (!task.WalltimeLimit.HasValue)
            throw new InvalidRequestException("TaskEmptyAttribute", "WalltimeLimit", task.Name);

        var remainingWalltime = (int)task.WalltimeLimit;
        var dividedExtraLongTasks = new List<TaskSpecification>();
        TaskDependency dependOnLast = null;
        var iteration = 0;
        //divide extra long task to n shorter tasks

        while (remainingWalltime > 0)
        {
            var shorterTask = new TaskSpecification(task);
            shorterTask.Name += $"_part_{++iteration}";
            shorterTask.DependsOn = new List<TaskDependency>();
            shorterTask.Depended = new List<TaskDependency>();

            //set maximal allowed or defined remaining WalltimeLimit for ExtraLong queue
            shorterTask.WalltimeLimit = remainingWalltime >= task.ClusterNodeType.MaxWalltime
                ? task.ClusterNodeType.MaxWalltime
                : remainingWalltime;
            remainingWalltime -= (int)shorterTask.WalltimeLimit;
            //create dependency on last task in the list
            if (dividedExtraLongTasks.Count > 0)
            {
                dependOnLast = new TaskDependency
                {
                    TaskSpecification = shorterTask,
                    ParentTaskSpecification = dividedExtraLongTasks.Last()
                };
                shorterTask.DependsOn.Add(dependOnLast);
            }
            else if (task.DependsOn != null && task.DependsOn.Count != 0)
            {
                foreach (var dependentsOnTask in task.DependsOn)
                {
                    //extraLong task depends on not extra long task
                    if (dependentsOnTask.ParentTaskSpecification.WalltimeLimit <
                        task.ClusterNodeType.MaxWalltime) //TODO CHECK THIS!
                        dependOnLast = new TaskDependency
                        {
                            TaskSpecification = shorterTask,
                            ParentTaskSpecification = dependentsOnTask.ParentTaskSpecification
                        };
                    else //task must be dependent on some previously decomposed extra long task (last task of decomposed sequence)
                        dependOnLast = new TaskDependency
                        {
                            TaskSpecification = shorterTask,
                            ParentTaskSpecification =
                                _extraLongTaskDecomposedDependency[dependentsOnTask.ParentTaskSpecification]
                        };
                    shorterTask.DependsOn.Add(dependOnLast);
                }
            }

            dividedExtraLongTasks.Add(shorterTask);
        }

        if (dividedExtraLongTasks.Count > 0)
        {
            //set Class private Lists to handle in the CreateJob method
            _extraLongTaskDecomposedDependency.Add(task, dividedExtraLongTasks.Last());
            _tasksToDeleteFromSpec.Add(task);
            _tasksToAddToSpec.AddRange(dividedExtraLongTasks);
        }
    }

    protected static IEnumerable<EnvironmentVariable> CombineJobAndTaskEnvironmentVariables(
        IEnumerable<EnvironmentVariable> jobVariables, IEnumerable<EnvironmentVariable> taskVariables)
    {
        Dictionary<string, EnvironmentVariable> globalVariables = new();
        foreach (var jobVariable in jobVariables ?? Enumerable.Empty<EnvironmentVariable>())
            globalVariables.TryAdd(jobVariable.Name, jobVariable);

        foreach (var taskVariable in taskVariables ?? Enumerable.Empty<EnvironmentVariable>())
            if (globalVariables.ContainsKey(taskVariable.Name))
                globalVariables[taskVariable.Name] = taskVariable;
            else
                globalVariables.Add(taskVariable.Name, taskVariable);
        return globalVariables.Values.ToList();
    }

    protected static SubmittedJobInfo CreateSubmittedJobInfo(JobSpecification specification)
    {
        SubmittedJobInfo result = new()
        {
            CreationTime = DateTime.UtcNow,
            Name = specification.Name,
            Project = specification.Project,
            Specification = specification,
            State = JobState.Configuring,
            Submitter = specification.Submitter,
            Tasks = specification.Tasks
                .OrderByDescending(x => x.Id)
                .Select(s => new SubmittedTaskInfo
                {
                    Name = s.Name,
                    Specification = s,
                    State = TaskState.Configuring,
                    Priority = s.Priority ?? TaskPriority.Average,
                    NodeType = s.ClusterNodeType,
                    Project = s.Project
                })
                .ToList()
        };
        return result;
    }

    internal static bool UpdateJobStateByTasks(SubmittedJobInfo dbJobInfo)
    {
        // TODO: review this method
        dbJobInfo.StartTime = dbJobInfo.Tasks.FirstOrDefault()?.StartTime;
        dbJobInfo.EndTime = dbJobInfo.Tasks.Where(t => t.EndTime.HasValue).LastOrDefault()?.EndTime;
        dbJobInfo.TotalAllocatedTime = dbJobInfo.Tasks.Sum(s => s.AllocatedTime ?? 0);

        var continuousJobState = JobState.Finished;
        var minTaskState = TaskState.Deleted;
        foreach (var task in dbJobInfo.Tasks)
        {
            if (task.State < minTaskState)
                minTaskState = task.State;

            if (task.State == TaskState.Failed)
            {
                continuousJobState = JobState.Failed;
            }
            else if (task.State == TaskState.Canceled && continuousJobState != JobState.Failed)
            {
                continuousJobState = JobState.Canceled;
            }
        }

        JobState newState = (JobState)minTaskState < JobState.Finished ? (JobState)minTaskState : continuousJobState;
        dbJobInfo.State = newState;
        return dbJobInfo.State != newState;
    }

    protected SubmittedJobInfo CombineSubmittedJobInfoFromCluster(SubmittedJobInfo dbJobInfo,
        IEnumerable<SubmittedTaskInfo> submittedTasksInfo)
    {
        try
        {
            var submittedTasksList = submittedTasksInfo.ToList();
            foreach (var dbTask in dbJobInfo.Tasks)
            {
                var taskIdStr = dbTask.Specification.Id.ToString();
                var matchingClusterTask = submittedTasksList.FirstOrDefault(f => f.Name == taskIdStr);
                
                if (matchingClusterTask == null)
                {
                    var availableNames = string.Join(", ", submittedTasksList.Select(t => $"'{t.Name}'"));
                    throw new Exception($"Task mapping failed. Could not find cluster task with Name matching DB TaskSpecification.Id '{taskIdStr}'. Available names from cluster: [{availableNames}]");
                }
                
                CombineSubmittedTaskInfoFromCluster(dbTask, matchingClusterTask);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error combining submitted job info from cluster for job {dbJobInfo.Id}: {ex.Message}");
            throw new InvalidRequestException("ErrorCombiningJobInfoFromCluster", ex.Message);
        }

        UpdateJobStateByTasks(dbJobInfo);
        JobCacheManager.InvalidateJobCache(dbJobInfo.Id);
        return dbJobInfo;
    }

    private static async Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksStateInHPCScheduler(
        Func<long, IRexScheduler> scheduler, IEnumerable<SubmittedTaskInfo> jobTasks, ClusterAuthenticationCredentials account, ILogger logger)
    {
        var unfinishedTasks = jobTasks
            .Where(w => w.State is > TaskState.Configuring and (<= TaskState.Running or TaskState.Canceled))
            .ToList();

        if (!unfinishedTasks.Any())
        {
            return Enumerable.Empty<SubmittedTaskInfo>();
        }

        var jobSpecification = unfinishedTasks.FirstOrDefault().Specification.JobSpecification;

        if (account == null)
        {
            account = jobSpecification.ClusterUser;
        }

        try
        {
            HEAppE.Utils.LoggingUtils.AddJobIdToLogThreadContext(jobSpecification.Id);
            logger.LogInformation($"Getting actual tasks state for job {jobSpecification.Id} using account {account?.Username}");
            return await scheduler(jobSpecification.Submitter.Id).GetActualTasksInfoAsync(unfinishedTasks, account, null, null);
        }
        finally
        {
            HEAppE.Utils.LoggingUtils.RemoveJobIdFromLogThreadContext();
        }
    }

    private static bool IsWaitingLimitExceeded(SubmittedJobInfo job)
    {
        if (job.Specification.WaitingLimit.HasValue && job.Specification.WaitingLimit > 0
                                                    && (job.State < JobState.Running ||
                                                        job.State == JobState.WaitingForServiceAccount))
        {
            var waitingLimit = job.Specification.WaitingLimit.Value;
            return DateTime.UtcNow.Subtract(job.SubmitTime.Value).TotalSeconds > waitingLimit;
        }

        return false;
    }

    protected SubmittedTaskInfo CombineSubmittedTaskInfoFromCluster(SubmittedTaskInfo dbTaskInfo,
        SubmittedTaskInfo clusterTaskInfo)
    {
        ResourceAccountingUtils.ComputeAccounting(dbTaskInfo, clusterTaskInfo, _logger, 
            taskId => _unitOfWork.SubmittedTaskInfoRepository.GetResourceConsumed(taskId));

        if (clusterTaskInfo is null)
        {
            dbTaskInfo.State = TaskState.Failed;
            return dbTaskInfo;
        }

        dbTaskInfo.TaskAllocationNodes = dbTaskInfo.TaskAllocationNodes?.Count > 0
            ? dbTaskInfo.TaskAllocationNodes
                .Union(clusterTaskInfo.TaskAllocationNodes, new SubmittedTaskAllocationNodeInfoComparer()).ToList()
            : dbTaskInfo.TaskAllocationNodes = clusterTaskInfo.TaskAllocationNodes;

        dbTaskInfo.ScheduledJobId = clusterTaskInfo.ScheduledJobId;
        dbTaskInfo.StartTime = clusterTaskInfo.StartTime;
        dbTaskInfo.EndTime = clusterTaskInfo.EndTime;
        dbTaskInfo.AllocatedTime = clusterTaskInfo.AllocatedTime;
        dbTaskInfo.AllocatedCores = clusterTaskInfo.AllocatedCores;
        dbTaskInfo.AllocatedGpus = clusterTaskInfo.AllocatedGpus;
        if (dbTaskInfo.State <= TaskState.Submitted || clusterTaskInfo.State > dbTaskInfo.State)
        {
            dbTaskInfo.State = clusterTaskInfo.State;
        }
        dbTaskInfo.AllParameters = clusterTaskInfo.AllParameters;
        dbTaskInfo.ErrorMessage = clusterTaskInfo.ErrorMessage;
        dbTaskInfo.Reason = clusterTaskInfo.Reason;
        return dbTaskInfo;
    }

    public async Task<SubmittedJobInfo> CreateJobDbRecord(JobSpecification specification, AdaptorUser loggedUser, bool isExtraLong)
    {
        var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
        var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
        
        if (LexisAuthenticationConfiguration.CheckCommandTemplatePermissions && !string.IsNullOrEmpty(_httpContextKeys.Context.LEXISToken))
        {
            string instanceId = HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath;
            CommandTemplatePermissionsModel permissionsModel = await _userOrgService.GetCommandTemplatePermissionsAsync(
                _httpContextKeys.Context.LEXISToken,
                HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath,
                instanceId, _logger);

            var project = _unitOfWork.ProjectRepository.GetByIdWithClusterProjects(specification.ProjectId)
                          ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound", specification.ProjectId);

            var cluster = project.ClusterProjects
                              .Select(cp => cp.Cluster)
                              .FirstOrDefault(c => c.Id == specification.ClusterId)
                          ?? throw new RequestedObjectDoesNotExistException("ClusterNotAssociatedWithProject", specification.ClusterId);

            foreach (var commandTemplateId in specification.Tasks.Select(s => s.CommandTemplateId).Distinct())
            {
                if (commandTemplateId.HasValue)
                {
                    var commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId.Value)
                                          ?? throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound", commandTemplateId.Value);

                    var queue = _unitOfWork.ClusterNodeTypeRepository.GetById(commandTemplate.ClusterNodeTypeId.Value)
                                ?? throw new RequestedObjectDoesNotExistException("ClusterNodeTypeNotExists", commandTemplate.ClusterNodeTypeId);

                _userOrgService.ValidatePermissions(
                    permissionsModel, 
                    cluster.Name, 
                    queue.Name, 
                    project.AccountingString, 
                    commandTemplate.Name,
                    _logger
                );
                }
            }
        }
        
        ClusterAuthenticationCredentials credentials = null;
        var targetCluster = clusterLogic.GetClusterById(specification.ClusterId);
        if (targetCluster != null && targetCluster.ConnectionProtocol != ClusterConnectionProtocol.Http && targetCluster.ConnectionProtocol != ClusterConnectionProtocol.Https)
        {
            credentials = await clusterLogic.GetNextAvailableUserCredentials(
                specification.ClusterId, specification.ProjectId, requireIsInitialized: true, adaptorUserId: loggedUser.Id);
        }
        else
        {
            try
            {
                credentials = await clusterLogic.GetNextAvailableUserCredentials(
                    specification.ClusterId, specification.ProjectId, requireIsInitialized: false, adaptorUserId: loggedUser.Id);
            }
            catch
            {
                // HTTP/HTTPS clusters do not require SSH credentials
            }
        }
        CompleteJobSpecification(specification, loggedUser, clusterLogic, userLogic, credentials);
        _logger.LogInformation($"User {loggedUser.GetLogIdentification()} is creating a job: {specification.ToLogSafeJsonString()}");

        foreach (var task in specification.Tasks)
        {
            if (isExtraLong) DecomposeExtraLongTask(task);
        }

        if (isExtraLong)
        {
            foreach (var task in _tasksToDeleteFromSpec) specification.Tasks.Remove(task);
            foreach (var task in _tasksToAddToSpec) specification.Tasks.Add(task);
        }

        var validator = new JobManagementValidator(specification, _unitOfWork, _sshCertificateAuthorityService,
            _httpContextKeys, _expirioService, _logger);
        var jobValidation = await validator.Validate();
        if (!jobValidation.IsValid)
            throw new InputValidationException("NotValidJobSpecification", jobValidation.Message);
        
        SubmittedJobInfo jobInfo = CreateSubmittedJobInfo(specification);
        _unitOfWork.JobSpecificationRepository.Insert(specification);
        _unitOfWork.SubmittedJobInfoRepository.Insert(jobInfo);

        await _unitOfWork.SaveAsync();

        return jobInfo;
    }

    public async Task DeleteJobDbRecord(long jobInfoId, long specificationId)
    {
        var jobInfo = await _unitOfWork.SubmittedJobInfoRepository.GetByIdWithTasksAsync(jobInfoId);
        if (jobInfo != null)
        {
            if (jobInfo.Tasks != null)
            {
                foreach (var task in jobInfo.Tasks.ToList())
                {
                    _unitOfWork.SubmittedTaskInfoRepository.Delete(task);
                }
            }
            _unitOfWork.SubmittedJobInfoRepository.Delete(jobInfo);
        }
        
        var specification = await _unitOfWork.JobSpecificationRepository.GetByIdAsync(specificationId);
        if (specification != null)
        {
            _unitOfWork.JobSpecificationRepository.Delete(specification);
        }
        
        await _unitOfWork.SaveAsync();
    }

    public async Task<(SubmittedJobInfo JobInfo, bool IsWaitingForServiceAccount)> PrepareJobForSubmitAsync(long createdJobInfoId, AdaptorUser loggedUser)
    {
        var jobInfo = await GetSubmittedJobInfoByIdForSubmitAsync(createdJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);

        foreach (var task in jobInfo.Tasks)
        {
            if (task.Specification?.EnvironmentVariables != null)
            {
                var sessionIdVar = task.Specification.EnvironmentVariables.FirstOrDefault(e => e.Name == "HEAPPE_QSCHEDULER_SESSION_ID");
                if (sessionIdVar != null && long.TryParse(sessionIdVar.Value, out var sid))
                {
                    await VerifySessionOwnerAsync(sid, loggedUser);
                    var session = await _unitOfWork.QSchedulerSessionRepository.GetBySessionIdAsync(sid);
                    if (session != null && session.State == QSchedulerSessionState.Closed)
                    {
                        throw new InvalidRequestException("SessionAlreadyClosed");
                    }
                }
            }
        }
        if (jobInfo.Specification.Tasks.Any(x => x.CommandTemplate != null && x.CommandTemplate.IsEnabled == false))
            throw new InvalidRequestException("CannotSubmitJobWithDisabledCommandTemplate");
        
        if (jobInfo.State != JobState.Configuring && jobInfo.State != JobState.WaitingForServiceAccount)
            throw new InputValidationException("SubmittingJobNotInConfiguringState");

        if (!BusinessLogicConfiguration.SharedAccountsPoolMode)
        {
            var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var isJobUserAvailable = clusterLogic.IsUserAvailableToRun(jobInfo.Specification.ClusterUser);

            if (!isJobUserAvailable)
            {
                jobInfo.State = JobState.WaitingForServiceAccount;
                await _unitOfWork.SaveAsync();
                return (jobInfo, true);
            }
        }
        var cluster = jobInfo.Specification.Cluster;
        var clusterConfig = ClusterRuntimeConfiguration.For(cluster.CustomConfiguration);
        if (clusterConfig.EnableCallback)
        {
            string masterKeyName = "ClusterCallbackNotifyToken";
            string? masterKey = await GetMasterCallbackTokenAsync(cluster);
                              
            if (string.IsNullOrEmpty(masterKey))
            {
                masterKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                
                bool callbackInVault = cluster.CustomConfigurationVaultToggles != null &&
                                       cluster.CustomConfigurationVaultToggles.TryGetValue(masterKeyName, out bool cVal) &&
                                       cVal;

                bool savedToVault = false;
                if (callbackInVault)
                {
                    var vaultConnector = new VaultConnector(_logger);
                    savedToVault = await vaultConnector.SetClusterSecretAsync(cluster.Id, masterKeyName, masterKey);
                    if (!savedToVault)
                    {
                        _logger.LogWarning($"Failed to save automatically generated {masterKeyName} to Vault for Cluster {cluster.Id}. Falling back to database.");
                    }
                }

                if (!savedToVault)
                {
                    if (cluster.CustomConfiguration == null) cluster.CustomConfiguration = new();
                    cluster.CustomConfiguration[masterKeyName] = masterKey;
                    await _unitOfWork.SaveAsync();
                }
            }

            foreach (var task in jobInfo.Tasks)
            {
                string taskToken = ComputeHmac(masterKey, task.Id.ToString());
                task.CallbackSecret = taskToken;
                if (task.Specification != null)
                {
                    task.Specification.CallbackSecret = taskToken;
                }
            }
        }

        return (jobInfo, false);
    }

    public async Task<SubmittedJobInfo> CompleteJobSubmitAsync(long createdJobInfoId, AdaptorUser loggedUser, IEnumerable<SubmittedTaskInfo> submittedTasks)
    {
        var jobInfo = await GetSubmittedJobInfoByIdForSubmitAsync(createdJobInfoId, loggedUser);
        jobInfo.SubmitTime = DateTime.UtcNow;

        var previousTaskStates = jobInfo.Tasks.ToDictionary(t => t.Id, t => t.State);
        var previousJobState = jobInfo.State;

        // Refresh task states from DB, bypassing the EF change-tracking cache.
        // A concurrent callback (separate UnitOfWork) may have already advanced a task
        // to Running/Finished while this UnitOfWork still holds the original Submitted state.
        // Without this reload, CombineSubmittedTaskInfoFromCluster would see the stale
        // Submitted state and SaveAsync would overwrite the callback's Finished state in DB.
        foreach (var task in jobInfo.Tasks)
        {
            var freshState = await _unitOfWork.SubmittedTaskInfoRepository.GetCurrentTaskStateAsync(task.Id);
            if (freshState.HasValue && freshState.Value > task.State)
            {
                _logger.LogDebug(
                    "CompleteJobSubmitAsync: Task {TaskId} state refreshed from EF-tracked {StaleState} to actual DB state {FreshState}.",
                    task.Id, task.State, freshState.Value);
                task.State = freshState.Value;
            }
        }

        jobInfo = CombineSubmittedJobInfoFromCluster(jobInfo, submittedTasks);
        await _unitOfWork.SaveAsync();

        // For QScheduler jobs using sessions: auto-register sessions created dynamically by the adapter.
        // The adapter creates sessions in QScheduler directly (without going through OpenQSchedulerSessionAsync),
        // so they are never stored in the QSchedulerSession table. We register them here, after the task
        // ScheduledJobIds are persisted, so that session callbacks (e.g. session:35 -> opened) can be resolved.
        if (jobInfo.Specification.Cluster.SchedulerType == SchedulerType.QScheduler)
        {
            foreach (var task in jobInfo.Tasks)
            {
                if (!string.IsNullOrEmpty(task.ScheduledJobId) && task.ScheduledJobId.StartsWith("session:"))
                {
                    // ScheduledJobId is either "session:35" (waiting) or "session:35:task:1101" (submitted)
                    var parts = task.ScheduledJobId.Split(':');
                    if (parts.Length >= 2 && long.TryParse(parts[1], out var sessionId))
                    {
                        var existingSession = await _unitOfWork.QSchedulerSessionRepository.GetBySessionIdAsync(sessionId);
                        if (existingSession == null)
                        {
                            _logger.LogInformation($"CompleteJobSubmitAsync: Auto-registering QScheduler session {sessionId} into QSchedulerSession table for task {task.Id}.");
                            _unitOfWork.QSchedulerSessionRepository.Insert(new QSchedulerSession
                            {
                                SessionId = sessionId,
                                ClusterId = jobInfo.Specification.ClusterId,
                                ProjectId = jobInfo.Project.Id,
                                UserId = loggedUser.Id,
                                State = QSchedulerSessionState.Waiting,
                                CreatedAt = DateTime.UtcNow
                            });
                            await _unitOfWork.SaveAsync();
                        }
                    }
                }
            }
        }

        await PublishStateChangesAsync(jobInfo, previousTaskStates, previousJobState);

        return jobInfo;
    }


    public async Task<(SubmittedJobInfo JobInfo, ClusterAuthenticationCredentials Credentials)> PrepareGetActualTasksInfoAsync(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        var jobInfo = await GetSubmittedJobInfoByIdAsync(submittedJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);

        var cluster = jobInfo.Specification.Cluster;
        var isQSchedulerHttp = cluster?.SchedulerType == SchedulerType.QScheduler && 
            (cluster.ConnectionProtocol == ClusterConnectionProtocol.Http || cluster.ConnectionProtocol == ClusterConnectionProtocol.Https);

        ClusterAuthenticationCredentials credentials = null;
        if (isQSchedulerHttp)
        {
            credentials = jobInfo.Specification.ClusterUser;
        }
        else
        {
            credentials = jobInfo.Specification.ClusterUser ?? await
                _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                    jobInfo.Specification.ClusterId, jobInfo.Specification.ProjectId, requireIsInitialized: true, adaptorUserId: loggedUser.Id, _logger);
        }
        return (jobInfo, credentials);
    }

    public async Task<SubmittedJobInfo> CompleteGetActualTasksInfoAsync(long submittedJobInfoId, AdaptorUser loggedUser, IEnumerable<SubmittedTaskInfo> actualTasksInfo)
    {
        var jobInfo = await GetSubmittedJobInfoByIdAsync(submittedJobInfoId, loggedUser);
        var actualUnfinishedSchedulerTasksInfo = actualTasksInfo.ToList();

        var previousTaskStates = jobInfo.Tasks.ToDictionary(t => t.Id, t => t.State);
        var previousJobState = jobInfo.State;

        foreach (var task in jobInfo.Tasks)
        {
            var actualUnfinishedSchedulerTaskInfo = actualUnfinishedSchedulerTasksInfo
                .FirstOrDefault(w => w.ScheduledJobId == task.ScheduledJobId);
            if (actualUnfinishedSchedulerTaskInfo != null)
            {
                CombineSubmittedTaskInfoFromCluster(task, actualUnfinishedSchedulerTaskInfo);
                task.StateSource = JobStateSource.UserVerified;
                task.StateUpdatedAt = DateTime.UtcNow;
            }
        }

        UpdateJobStateByTasks(jobInfo);
        jobInfo.StateSource = JobStateSource.UserVerified;
        jobInfo.StateUpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveAsync();
        await CheckAndCloseQSchedulerSessionsAsync(jobInfo);

        await PublishStateChangesAsync(jobInfo, previousTaskStates, previousJobState);

        return jobInfo;
    }

    public async Task<(SubmittedJobInfo JobInfo, ClusterAuthenticationCredentials Credentials, bool CancelledLocally)> PrepareCancelJobAsync(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        var jobInfo = await GetSubmittedJobInfoByIdAsync(submittedJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);
        if (jobInfo.State is >= JobState.Submitted and < JobState.Finished)
        {
            var cluster = jobInfo.Specification.Cluster;
            var isQSchedulerHttp = cluster?.SchedulerType == SchedulerType.QScheduler && 
                (cluster.ConnectionProtocol == ClusterConnectionProtocol.Http || cluster.ConnectionProtocol == ClusterConnectionProtocol.Https);

            ClusterAuthenticationCredentials credentials = null;
            if (!isQSchedulerHttp)
            {
                credentials = await
                    _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                        jobInfo.Specification.ClusterId, jobInfo.Specification.ProjectId, requireIsInitialized: true, adaptorUserId: loggedUser.Id, logger: _logger);
            }
            return (jobInfo, credentials, false);
        }
        else if (jobInfo.State is JobState.WaitingForServiceAccount || jobInfo.State is JobState.Configuring)
        {
            jobInfo.State = JobState.Canceled;
            jobInfo.Tasks.ForEach(f => f.State = TaskState.Canceled);
            await _unitOfWork.SaveAsync();
            return (jobInfo, null, true);
        }
        else
        {
            throw new InvalidRequestException("CannotCancelJob", submittedJobInfoId, jobInfo.State);
        }
    }

    public async Task<SubmittedJobInfo> CompleteCancelJobAsync(long submittedJobInfoId, AdaptorUser loggedUser, IEnumerable<SubmittedTaskInfo> actualTasksInfo)
    {
        var jobInfo = await GetSubmittedJobInfoByIdAsync(submittedJobInfoId, loggedUser);
        var actualUnfinishedSchedulerTasksInfo = actualTasksInfo.ToList();

        var previousTaskStates = jobInfo.Tasks.ToDictionary(t => t.Id, t => t.State);
        var previousJobState = jobInfo.State;

        // O(N) dictionary lookup instead of O(N²) nested foreach.
        // Each DB task is matched to its corresponding cluster task by ScheduledJobId.
        var schedulerTaskByJobId = actualUnfinishedSchedulerTasksInfo
            .Where(t => t.ScheduledJobId != null)
            .ToDictionary(t => t.ScheduledJobId!);

        foreach (var task in jobInfo.Tasks)
        {
            if (task.ScheduledJobId != null && schedulerTaskByJobId.TryGetValue(task.ScheduledJobId, out var clusterTask))
                CombineSubmittedTaskInfoFromCluster(task, clusterTask);
        }

        UpdateJobStateByTasks(jobInfo);
        await _unitOfWork.SaveAsync();

        await PublishStateChangesAsync(jobInfo, previousTaskStates, previousJobState);

        return jobInfo;
    }

    public async Task<(SubmittedJobInfo JobInfo, ClusterProject ClusterProject)> PrepareDeleteJobAsync(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        var jobInfo = await GetSubmittedJobInfoByIdAsync(submittedJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);
        var clusterProject =
            _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(jobInfo.Specification.ClusterId,
                jobInfo.Project.Id) ?? throw new InvalidRequestException("NotExistingProject");

        if (jobInfo.State is JobState.Configuring
            or >= JobState.Finished and not JobState.WaitingForServiceAccount and not JobState.Deleted)
        {
            return (jobInfo, clusterProject);
        }

        throw new InvalidRequestException("CannotDeleteJob", submittedJobInfoId, jobInfo.State);
    }

    public async Task<bool> CompleteDeleteJobAsync(long submittedJobInfoId, AdaptorUser loggedUser, bool isDeleted)
    {
        if (isDeleted)
        {
            var jobInfo = await GetSubmittedJobInfoByIdAsync(submittedJobInfoId, loggedUser);
            jobInfo.State = JobState.Deleted;
            jobInfo.Tasks.ForEach(f => f.State = TaskState.Deleted);
            await _unitOfWork.SaveAsync();
        }
        return isDeleted;
    }

    public async Task<(SubmittedJobInfo JobInfo, string LocalBasePath, string JobLogArchivePath, IEnumerable<System.Tuple<string, string>> SourceDestinations)> PrepareArchiveJobAsync(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        var jobInfo = await GetSubmittedJobInfoByIdAsync(submittedJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);
        
        var basePath = jobInfo.Specification.Cluster.ClusterProjects
            .Find(cp => cp.ProjectId == jobInfo.Specification.ProjectId)?.ScratchStoragePath;
        var projectBasePath = jobInfo.Specification.Cluster.ClusterProjects
            .Find(cp => cp.ProjectId == jobInfo.Specification.ProjectId)?.ProjectStoragePath;
        if (string.IsNullOrEmpty(projectBasePath))
        {
            projectBasePath = basePath;
        }

        basePath = FileSystemUtils.ExpandRemotePath(basePath, jobInfo.Specification.ClusterUser.Username, null, jobInfo.Specification.Cluster?.CustomConfiguration);
        projectBasePath = FileSystemUtils.ExpandRemotePath(projectBasePath, jobInfo.Specification.ClusterUser.Username, null, jobInfo.Specification.Cluster?.CustomConfiguration);
        
        var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        var localBasePath = Path.Combine(
                basePath, 
                clusterConfig.InstanceIdentifierPath, 
                clusterConfig.SubExecutionsPath.TrimStart('/'),
                jobInfo.Specification.ClusterUser.Username);
        var jobLogArchivePath = Path.Combine(
                projectBasePath, 
                clusterConfig.InstanceIdentifierPath, 
                clusterConfig.JobLogArchiveSubPath.TrimStart('/'), 
                jobInfo.Specification.ClusterUser.Username);
        
        var sourceDestinations = jobInfo.Specification.Tasks
            .SelectMany(x => new[]
            {
                CreatePathTuple(localBasePath, jobLogArchivePath, x, x.StandardOutputFile),
                CreatePathTuple(localBasePath, jobLogArchivePath, x, x.StandardErrorFile),
            }).ToList();

        return (jobInfo, localBasePath, jobLogArchivePath, sourceDestinations);
    }

    public async Task<SubmittedTaskInfo> PrepareGetAllocatedNodesIPsAsync(long submittedTaskInfoId, AdaptorUser loggedUser)
    {
        var taskInfo = await GetSubmittedTaskInfoByIdAsync(submittedTaskInfoId, loggedUser);
        VerifyOwner(taskInfo.Specification.JobSpecification, loggedUser);
        if (taskInfo.State != TaskState.Running)
            throw new InputValidationException("IPAddressesProvidedOnlyForRunningTask");
        return taskInfo;
    }

    public async Task<(DryRunJobSpecification Specification, Cluster Cluster, Project Project)> PrepareDryRunJobAsync(
        long modelProjectId, long modelClusterNodeTypeId, long modelNodes, long modelTasksPerNode, long modelWallTimeInMinutes, AdaptorUser loggedUser)
    {
        var project = await _unitOfWork.ProjectRepository.GetByIdWithClusterProjectsAsync(modelProjectId)
                      ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound", modelProjectId);
        var clusterNodeType = await _unitOfWork.ClusterNodeTypeRepository.GetByIdWithClusterAndProjectsAsync(modelClusterNodeTypeId)
                              ?? throw new RequestedObjectDoesNotExistException("ClusterNodeTypeNotExists",
                                  modelClusterNodeTypeId);
        var cluster = clusterNodeType.Cluster;

        var dryRunJobSpecification = new DryRunJobSpecification
        {
            Project = project,
            ClusterNodeType = clusterNodeType,
            Nodes = modelNodes,
            TasksPerNode = modelTasksPerNode,
            WallTimeInMinutes = modelWallTimeInMinutes,
            IsGpuPartition = clusterNodeType.ClusterNodeTypeAggregation != null && (clusterNodeType.ClusterNodeTypeAggregation.AllocationType.Contains("ACN", StringComparison.OrdinalIgnoreCase) || clusterNodeType.ClusterNodeTypeAggregation.AllocationType.Contains("GPU", StringComparison.OrdinalIgnoreCase)),
            ClusterUser = await LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
                .GetNextAvailableUserCredentials(cluster.Id, project.Id, requireIsInitialized: true, adaptorUserId: loggedUser.Id)
        };

        return (dryRunJobSpecification, cluster, project);
    }

    public async Task<(SubmittedJobInfo JobInfo, ClusterProject ClusterProject)> PrepareCopyJobDataToTempAsync(long createdJobInfoId, AdaptorUser loggedUser)
    {
        var jobInfo = await GetSubmittedJobInfoByIdAsync(createdJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);
        var clusterProject =
            _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(jobInfo.Specification.ClusterId,
                jobInfo.Project.Id) ?? throw new InvalidRequestException("NotExistingProject");
        return (jobInfo, clusterProject);
    }

    public async Task<(SubmittedJobInfo JobInfo, ClusterProject ClusterProject)> PrepareCopyJobDataFromTempAsync(long createdJobInfoId, AdaptorUser loggedUser)
    {
        var jobInfo = await GetSubmittedJobInfoByIdAsync(createdJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);
        var clusterProject =
            _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(jobInfo.Specification.ClusterId,
                jobInfo.Project.Id) ?? throw new InvalidRequestException("NotExistingProject");
        return (jobInfo, clusterProject);
    }

    private static void VerifyOwner(SubmittedJobInfo jobInfo, AdaptorUser loggedUser)
    {
        if (jobInfo.Submitter.Id != loggedUser.Id)
        {
            throw new AdaptorUserNotAuthorizedForJobException("ClusterOperationRequiresOwner", loggedUser.GetLogIdentification(), jobInfo.Id);
        }
    }

    private static void VerifyOwner(JobSpecification jobSpec, AdaptorUser loggedUser)
    {
        if (jobSpec.Submitter.Id != loggedUser.Id)
        {
            throw new AdaptorUserNotAuthorizedForJobException("ClusterOperationRequiresOwner", loggedUser.GetLogIdentification(), jobSpec.Id);
        }
    }
    public async Task<long> ProcessTaskCallbackAsync(string scheduledJobId, string token, string? rawResponse, string? qSchedulerState)
    {
        _logger.LogDebug($"ProcessTaskCallbackAsync: Callback received for scheduledJobId '{scheduledJobId}', qSchedulerState '{qSchedulerState}'");
        // 1. Find all potential candidate tasks matching this scheduledJobId
        var candidates = await _unitOfWork.SubmittedTaskInfoRepository.GetTasksByScheduledJobIdAsync(scheduledJobId);
        if (candidates == null || !candidates.Any())
        {
            if (scheduledJobId.StartsWith("session:"))
            {
                var sessionHandler = _callbackHandlers[SchedulerType.QScheduler];
                return await sessionHandler.ProcessSessionCallbackAsync(scheduledJobId, token, qSchedulerState);
            }

            _logger.LogWarning($"ProcessTaskCallbackAsync: Session callback received for '{scheduledJobId}' but no matching task or session found in DB. Acknowledging with success.");
            return 0;
        }

        QSchedulerSession? dbSession = null;
        if (scheduledJobId.StartsWith("session:"))
        {
            var parts = scheduledJobId.Split(':');
            if (parts.Length >= 2 && long.TryParse(parts[1], out var sessionId))
            {
                dbSession = await _unitOfWork.QSchedulerSessionRepository.GetBySessionIdAsync(sessionId);
            }
        }

        SubmittedTaskInfo? dbTask = null;
        SubmittedJobInfo? jobInfo = null;
        Cluster? cluster = null;
        Callbacks.ISchedulerCallbackHandler? handler = null;

        var sortedCandidates = candidates.OrderBy(c => c.State >= TaskState.Finished).ToList();

        foreach (var candidate in sortedCandidates)
        {
            if (candidate.State >= TaskState.Finished)
            {
                _logger.LogInformation($"ProcessTaskCallbackAsync: Skipping candidate task {candidate.Id} because it is in a final state: {candidate.State}.");
                continue;
            }

            var job = await _unitOfWork.SubmittedJobInfoRepository.GetByIdWithTasksAsync(candidate.Specification.JobSpecification.Id);
            if (job == null) continue;

            if ((job.Submitter != null && job.Submitter.IsBlocked) || (job.Specification?.Submitter != null && job.Specification.Submitter.IsBlocked))
            {
                _logger.LogWarning($"ProcessTaskCallbackAsync: Skipping candidate task {candidate.Id} because job owner '{job.Submitter?.Username ?? job.Specification?.Submitter?.Username}' is blocked.");
                continue;
            }

            if (dbSession != null && (job.SubmitTime == null || job.SubmitTime < dbSession.CreatedAt - TimeSpan.FromSeconds(10)))
            {
                _logger.LogInformation($"ProcessTaskCallbackAsync: Skipping candidate task {candidate.Id} because job {job.Id} submit time ({job.SubmitTime:O}) is older than session creation time ({dbSession.CreatedAt:O}).");
                continue;
            }

            var currentCluster = job.Specification.Cluster;
            if (_callbackHandlers.TryGetValue(currentCluster.SchedulerType, out var currentHandler))
            {
                bool isAuthenticated = await currentHandler.AuthenticateTaskCallbackAsync(token, candidate, job, currentCluster);
                if (isAuthenticated)
                {
                    dbTask = job.Tasks.FirstOrDefault(t => t.Id == candidate.Id) ?? candidate;
                    jobInfo = job;
                    cluster = currentCluster;
                    handler = currentHandler;
                    break;
                }
            }
        }

        _logger.LogDebug($"ProcessTaskCallbackAsync: candidate count={candidates.Count}, dbTask ID={dbTask?.Id}, dbTask State={dbTask?.State}, jobInfo ID={jobInfo?.Id}, jobInfo State={jobInfo?.State}");

        if (dbTask == null || jobInfo == null || cluster == null || handler == null)
        {
            if (scheduledJobId.StartsWith("session:"))
            {
                var sessionHandler = _callbackHandlers[SchedulerType.QScheduler];
                return await sessionHandler.ProcessSessionCallbackAsync(scheduledJobId, token, qSchedulerState);
            }

            throw new UnauthorizedAccessException("Authentication failed: Invalid callback token.");
        }

        var callbackClusterConfig = ClusterRuntimeConfiguration.For(cluster.CustomConfiguration);
        if (!callbackClusterConfig.EnableCallback)
        {
            _logger.LogDebug($"ProcessTaskCallbackAsync: EnableCallback is false for cluster {cluster.Id}. Ignoring callback – relying on polling.");
            return 0;
        }

        var result = await handler.ProcessTaskCallbackAsync(rawResponse, qSchedulerState, dbTask, jobInfo, cluster);
        _logger.LogDebug($"ProcessTaskCallbackAsync: ProcessTaskCallbackAsync finished. result.Handled={result.Handled}, result.TargetState={result.TargetState}");
        if (result.Handled)
        {
            return result.JobId;
        }

        _logger.LogDebug($"ProcessTaskCallbackAsync: Checking transition: TargetState={result.TargetState}, CurrentState={dbTask.State}");

        if (result.TargetState != TaskState.Unknown && dbTask.State < TaskState.Finished)
        {
            _logger.LogDebug($"ProcessTaskCallbackAsync: Entering state transition block for task {dbTask.Id}");
            var previousTaskStates = jobInfo.Tasks.ToDictionary(t => t.Id, t => t.State);
            var previousJobState = jobInfo.State;

            dbTask.State = result.TargetState;
            dbTask.StateSource = JobStateSource.Callback;
            dbTask.StateUpdatedAt = DateTime.UtcNow;
            dbTask.ErrorMessage = result.ErrorMessage ?? dbTask.ErrorMessage;
            dbTask.Reason = result.Reason ?? dbTask.Reason;
            dbTask.AllParameters = result.AllParameters ?? dbTask.AllParameters;
            dbTask.AllocatedTime = result.AllocatedTime ?? dbTask.AllocatedTime;
            
            if (result.TargetState == TaskState.Running)
            {
                dbTask.StartTime = result.StartTime ?? dbTask.StartTime ?? DateTime.UtcNow;
            }
            else if (result.TargetState >= TaskState.Finished)
            {
                dbTask.EndTime = result.EndTime ?? DateTime.UtcNow;
                dbTask.StartTime = result.StartTime ?? dbTask.StartTime ?? jobInfo.SubmitTime ?? DateTime.UtcNow;
            }

            UpdateJobStateByTasks(jobInfo);
            jobInfo.StateSource = JobStateSource.Callback;
            jobInfo.StateUpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveAsync();
            JobCacheManager.InvalidateJobCache(jobInfo.Id);
            await handler.PostProcessCallbackAsync(dbTask, jobInfo, cluster);

            await PublishStateChangesAsync(jobInfo, previousTaskStates, previousJobState);
        }

        return jobInfo.Id;
    }


    internal async Task CheckAndCloseQSchedulerSessionsAsync(SubmittedJobInfo jobInfo)
    {
        if (jobInfo?.Specification?.Cluster?.SchedulerType != SchedulerType.QScheduler)
        {
            return;
        }

        // Bypass session closure if this job submitted to an externally managed session
        var firstTask = jobInfo.Tasks.FirstOrDefault();
        if (firstTask?.Specification?.EnvironmentVariables != null &&
            firstTask.Specification.EnvironmentVariables.Any(e => e.Name == "HEAPPE_QSCHEDULER_SESSION_ID"))
        {
            _logger.LogInformation($"Job {jobInfo.Id} is submitted to externally managed session. Skipping automatic session closure.");
            return;
        }

        var sessionsToCheck = new HashSet<string>();
        foreach (var task in jobInfo.Tasks)
        {
            if (!string.IsNullOrEmpty(task.ScheduledJobId) && task.ScheduledJobId.StartsWith("session:"))
            {
                string sessionId;
                if (task.ScheduledJobId.Contains(":task:"))
                {
                    sessionId = task.ScheduledJobId.Split(new[] { ":task:" }, StringSplitOptions.None)[0].Substring("session:".Length);
                }
                else
                {
                    sessionId = task.ScheduledJobId.Substring("session:".Length);
                }
                sessionsToCheck.Add(sessionId);
            }
        }

        foreach (var sessionIdStr in sessionsToCheck)
        {
            if (long.TryParse(sessionIdStr, out var sessionId))
            {
                var sessionTasks = await _unitOfWork.SubmittedTaskInfoRepository.GetTasksByQSchedulerSessionIdAsync(sessionId);

                if (sessionTasks.Any() && sessionTasks.All(t => IsFinalTaskState(t.State)))
            {
                _logger.LogInformation($"All tasks in QScheduler session '{sessionId}' have completed. Closing session.");
                try
                {
                    ClusterAuthenticationCredentials credentials = null;
                    var cluster = jobInfo.Specification.Cluster;
                    var isQSchedulerHttp = cluster?.SchedulerType == SchedulerType.QScheduler && 
                        (cluster.ConnectionProtocol == ClusterConnectionProtocol.Http || cluster.ConnectionProtocol == ClusterConnectionProtocol.Https);

                    if (!isQSchedulerHttp)
                    {
                        if (jobInfo.Specification.ClusterUser?.AuthenticationType == ClusterAuthenticationCredentialsAuthType.Kerberos)
                        {
                            credentials = jobInfo.Specification.ClusterUser;
                        }
                        else
                        {
                            credentials = await _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                                jobInfo.Specification.ClusterId, jobInfo.Specification.ProjectId, requireIsInitialized: true,
                                adaptorUserId: jobInfo.Submitter.Id, _logger);
                        }
                    }

                    var scheduler = SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
                        .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService,
                            jobInfo.Submitter.Id, _expirioService, _expirioToken, _logger);

                    var dummyTask = new SubmittedTaskInfo
                    {
                        ScheduledJobId = $"session:{sessionId}",
                        NodeType = new ClusterNodeType { Cluster = jobInfo.Specification.Cluster },
                        Specification = new TaskSpecification
                        {
                            JobSpecification = jobInfo.Specification
                        }
                    };
                    await scheduler.CancelJobAsync(new List<SubmittedTaskInfo> { dummyTask }, "Auto-closing completed session.",
                        credentials, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

                    _logger.LogInformation($"Successfully requested QScheduler session '{sessionId}' closure.");

                    var dbSession = await _unitOfWork.QSchedulerSessionRepository.GetBySessionIdAsync(sessionId);
                    if (dbSession != null && dbSession.State != QSchedulerSessionState.Closed)
                    {
                        dbSession.State = QSchedulerSessionState.Closed;
                        dbSession.ClosedAt = DateTime.UtcNow;
                        await _unitOfWork.SaveAsync();
                        JobCacheManager.InvalidateJobCache(jobInfo.Id);

                        await PublishEventAsync(dbSession.UserId, "org.heappe.session.state-changed", "/heappe/sessions", new
                        {
                            sessionId = $"session:{sessionId}",
                            state = "Closed"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to auto-close completed QScheduler session '{sessionId}'.");
                }
            }
        }
    }
}

    private static bool IsFinalTaskState(TaskState state)
    {
        return state == TaskState.Finished ||
               state == TaskState.Failed ||
               state == TaskState.Canceled ||
               state == TaskState.Deleted;
    }

    internal async Task PublishStateChangesAsync(SubmittedJobInfo jobInfo, Dictionary<long, TaskState> previousTaskStates, JobState previousJobState)
    {
        if (jobInfo == null) return;
        var userId = jobInfo.Submitter.Id;

        // Check task state changes
        foreach (var task in jobInfo.Tasks)
        {
            var oldState = previousTaskStates.TryGetValue(task.Id, out var state) ? state : TaskState.Unknown;
            if (task.State != oldState)
            {
                await PublishEventAsync(userId, "org.heappe.task.state-changed", "/heappe/tasks", new
                {
                    jobId = jobInfo.Id,
                    taskId = task.Id,
                    state = task.State.ToString(),
                    errorMessage = task.ErrorMessage
                });
            }
        }

        // Check job state changes
        if (jobInfo.State != previousJobState)
        {
            await PublishEventAsync(userId, "org.heappe.job.state-changed", "/heappe/jobs", new
            {
                jobId = jobInfo.Id,
                state = jobInfo.State.ToString()
            });
        }
    }

    internal async Task PublishEventAsync(long userId, string eventType, string source, object data)
    {
        try
        {
            var eventHub = LogicFactory.GetService<IHEAppEEventHub>();
            if (eventHub != null)
            {
                await eventHub.PublishEventAsync(userId, eventType, source, data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"[JobManagementLogic] Failed to publish event '{eventType}': {ex.Message}");
        }
    }

    private async Task VerifySessionOwnerAsync(long sessionId, AdaptorUser loggedUser)
    {
        var session = await _unitOfWork.QSchedulerSessionRepository.GetBySessionIdAsync(sessionId);

        if (session == null)
        {
            _logger.LogWarning($"Session {sessionId} has no tracking record in HEAppE database.");
            return;
        }

        if (session.UserId != loggedUser.Id)
        {
            _logger.LogError($"User {loggedUser.Id} attempted to use session {sessionId} owned by user {session.UserId}.");
            throw new Exceptions.External.InvalidRequestException("SessionOwnershipMismatch");
        }

        var hasSubmitterRoleInProject = loggedUser.AdaptorUserUserGroupRoles?.Any(r =>
            r.AdaptorUserRole?.ContainedRoleTypes?.Contains(AdaptorUserRoleType.Submitter) == true &&
            r.AdaptorUserGroup?.ProjectId == session.ProjectId &&
            r.IsDeleted == false) == true;

        if (!hasSubmitterRoleInProject)
        {
            _logger.LogError($"User {loggedUser.Id} does not have Submitter role in project {session.ProjectId} of session {sessionId}.");
            throw new Exceptions.External.InvalidRequestException("InsufficientRoleForSessionProject");
        }
    }

    public async Task<long> OpenQSchedulerSessionAsync(long clusterId, long projectId, string machineId, int walltimeLimitSecs, AdaptorUser loggedUser)
    {
        var cluster = await _unitOfWork.ClusterRepository.GetByIdAsync(clusterId) 
            ?? throw new Exceptions.External.InvalidRequestException("NotExistingCluster");
        var project = await _unitOfWork.ProjectRepository.GetByIdWithAggregationsAsync(projectId)
            ?? throw new Exceptions.External.InvalidRequestException("NotExistingProject");

        if (cluster.SchedulerType != SchedulerType.QScheduler)
        {
            throw new Exceptions.External.InvalidRequestException("ClusterIsNotQScheduler");
        }

        ClusterAuthenticationCredentials credentials = null;
        if (cluster.ConnectionProtocol != ClusterConnectionProtocol.Http && cluster.ConnectionProtocol != ClusterConnectionProtocol.Https)
        {
            var clusterInfoLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            credentials = await clusterInfoLogic.GetNextAvailableUserCredentials(cluster.Id, project.Id, requireIsInitialized: true, adaptorUserId: loggedUser.Id);
        }

        var scheduler = HpcConnectionFramework.SchedulerAdapters.SchedulerFactory.GetInstance(cluster.SchedulerType)
            .CreateScheduler(cluster, project, _sshCertificateAuthorityService, loggedUser.Id, _expirioService, _expirioToken, _logger);

        var sessionId = await scheduler.OpenSessionAsync(cluster, machineId, project.AccountingString, walltimeLimitSecs, credentials, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        _unitOfWork.QSchedulerSessionRepository.Insert(new QSchedulerSession
        {
            SessionId = sessionId,
            ClusterId = clusterId,
            ProjectId = projectId,
            UserId = loggedUser.Id,
            State = QSchedulerSessionState.Waiting,
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.SaveAsync();

        await PublishEventAsync(loggedUser.Id, "org.heappe.session.state-changed", "/heappe/sessions", new
        {
            sessionId = $"session:{sessionId}",
            state = "Waiting"
        });

        return sessionId;
    }

    public async Task CloseQSchedulerSessionAsync(long clusterId, long projectId, long sessionId, AdaptorUser loggedUser)
    {
        var cluster = await _unitOfWork.ClusterRepository.GetByIdAsync(clusterId) 
            ?? throw new Exceptions.External.InvalidRequestException("NotExistingCluster");
        var project = await _unitOfWork.ProjectRepository.GetByIdWithClusterProjectsAsync(projectId)
            ?? throw new Exceptions.External.InvalidRequestException("NotExistingProject");

        if (cluster.SchedulerType != SchedulerType.QScheduler)
        {
            throw new Exceptions.External.InvalidRequestException("ClusterIsNotQScheduler");
        }

        var session = await _unitOfWork.QSchedulerSessionRepository.GetBySessionIdAsync(sessionId);
        if (session != null && session.State == QSchedulerSessionState.Closed)
        {
            _logger.LogInformation($"CloseQSchedulerSessionAsync: Session {sessionId} is already closed in database. Skipping close request.");
            return;
        }

        await VerifySessionOwnerAsync(sessionId, loggedUser);

        ClusterAuthenticationCredentials credentials = null;
        if (cluster.ConnectionProtocol != ClusterConnectionProtocol.Http && cluster.ConnectionProtocol != ClusterConnectionProtocol.Https)
        {
            var clusterInfoLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            credentials = await clusterInfoLogic.GetNextAvailableUserCredentials(cluster.Id, project.Id, requireIsInitialized: true, adaptorUserId: loggedUser.Id);
        }

        var scheduler = HpcConnectionFramework.SchedulerAdapters.SchedulerFactory.GetInstance(cluster.SchedulerType)
            .CreateScheduler(cluster, project, _sshCertificateAuthorityService, loggedUser.Id, _expirioService, _expirioToken, _logger);

        await scheduler.CloseSessionAsync(cluster, sessionId, credentials, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
    }
#pragma warning disable IDE1006
    private string _expirioToken
    {
        get => !string.IsNullOrEmpty(_httpContextKeys.Context.LEXISToken) ? _httpContextKeys.Context.LEXISToken : _httpContextKeys.Context.IdpToken;
    }
#pragma warning restore IDE1006

    public async Task<QSchedulerSession> GetQSchedulerSessionInfoAsync(long sessionId, AdaptorUser loggedUser)
    {
        var session = await _unitOfWork.QSchedulerSessionRepository.GetBySessionIdAsync(sessionId)
            ?? throw new Exceptions.External.RequestedObjectDoesNotExistException("NotExistingQSchedulerSession", sessionId);

        if (session.UserId != loggedUser.Id)
        {
            _logger.LogError($"User {loggedUser.Id} attempted to read session {sessionId} owned by user {session.UserId}.");
            throw new Exceptions.External.InvalidRequestException("SessionOwnershipMismatch");
        }

        var hasSubmitterRoleInProject = loggedUser.AdaptorUserUserGroupRoles?.Any(r =>
            r.AdaptorUserRole?.ContainedRoleTypes?.Contains(AdaptorUserRoleType.Submitter) == true &&
            r.AdaptorUserGroup?.ProjectId == session.ProjectId &&
            r.IsDeleted == false) == true;

        if (!hasSubmitterRoleInProject)
        {
            _logger.LogError($"User {loggedUser.Id} does not have Submitter role in project {session.ProjectId} of session {sessionId}.");
            throw new Exceptions.External.InvalidRequestException("InsufficientRoleForSessionProject");
        }

        return session;
    }

    public async Task<System.Collections.Generic.IEnumerable<QSchedulerSession>> ListQSchedulerSessionsAsync(AdaptorUser loggedUser, QSchedulerSessionState? state = null, long? clusterId = null, long? projectId = null)
    {
        var sessions = await _unitOfWork.QSchedulerSessionRepository.ListSessionsAsync(loggedUser.Id, state, clusterId, projectId);
        
        // Filter sessions where the user has Submitter role in the session's project
        var authorizedSessions = new List<QSchedulerSession>();
        foreach (var session in sessions)
        {
            var hasSubmitterRoleInProject = loggedUser.AdaptorUserUserGroupRoles?.Any(r =>
                r.AdaptorUserRole?.ContainedRoleTypes?.Contains(AdaptorUserRoleType.Submitter) == true &&
                r.AdaptorUserGroup?.ProjectId == session.ProjectId &&
                r.IsDeleted == false) == true;

            if (hasSubmitterRoleInProject)
            {
                authorizedSessions.Add(session);
            }
        }

        return authorizedSessions;
    }

    internal async Task<string?> GetMasterCallbackTokenAsync(Cluster cluster)
    {
        string? token = null;

        // 1. Check if ClusterCallbackNotifyToken is in Vault
        bool callbackInVault = cluster.CustomConfigurationVaultToggles != null &&
                               cluster.CustomConfigurationVaultToggles.TryGetValue("ClusterCallbackNotifyToken", out bool cVal) &&
                               cVal;
        if (callbackInVault)
        {
            var vaultConnector = new VaultConnector(_logger);
            token = await vaultConnector.GetClusterSecretAsync(cluster.Id, "ClusterCallbackNotifyToken");
            if (!string.IsNullOrEmpty(token)) return token;
        }

        // 2. Check if QSchedulerNotifyToken is in Vault
        bool qSchedulerInVault = cluster.CustomConfigurationVaultToggles != null &&
                                 cluster.CustomConfigurationVaultToggles.TryGetValue("QSchedulerNotifyToken", out bool qVal) &&
                                 qVal;
        if (qSchedulerInVault)
        {
            var vaultConnector = new VaultConnector(_logger);
            token = await vaultConnector.GetClusterSecretAsync(cluster.Id, "QSchedulerNotifyToken");
            if (!string.IsNullOrEmpty(token)) return token;
        }

        // 3. Fallback to CustomConfiguration dictionary
        if (cluster.CustomConfiguration != null)
        {
            if (cluster.CustomConfiguration.TryGetValue("ClusterCallbackNotifyToken", out token) && !string.IsNullOrEmpty(token))
            {
                return token;
            }
            if (cluster.CustomConfiguration.TryGetValue("QSchedulerNotifyToken", out token) && !string.IsNullOrEmpty(token))
            {
                return token;
            }
        }

        return null;
    }

    internal static string ComputeHmac(string key, string message)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        using (var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes))
        {
            var hashBytes = hmac.ComputeHash(messageBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }

    public async Task<System.IO.Stream> GetQuantumTaskResultAsync(long submittedTaskId, AdaptorUser loggedUser)
    {
        var taskInfo = await GetSubmittedTaskInfoByIdAsync(submittedTaskId, loggedUser, checkSharedJobInfoAccess: true);

        var jobInfo = await _unitOfWork.SubmittedJobInfoRepository.GetJobsQuery()
            .Include(j => j.Specification)
                .ThenInclude(s => s.Cluster)
            .Include(j => j.Project)
                .ThenInclude(p => p.ClusterProjects)
                    .ThenInclude(cp => cp.ClusterProjectCredentials)
            .FirstOrDefaultAsync(j => j.Tasks.Any(t => t.Id == submittedTaskId))
            ?? throw new Exceptions.External.RequestedObjectDoesNotExistException("NotExistingJobInfo", submittedTaskId);

        var jobSpecification = jobInfo.Specification;
        if (jobSpecification.Cluster?.SchedulerType != SchedulerType.QScheduler)
        {
            throw new Exceptions.External.InvalidRequestException("TaskNotOnQSchedulerCluster");
        }

        ClusterAuthenticationCredentials credentials = null;
        if (jobSpecification.Cluster.ConnectionProtocol != ClusterConnectionProtocol.Http &&
            jobSpecification.Cluster.ConnectionProtocol != ClusterConnectionProtocol.Https)
        {
            var clusterInfoLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            credentials = await clusterInfoLogic.GetNextAvailableUserCredentials(jobSpecification.Cluster.Id, jobInfo.Project.Id, requireIsInitialized: true, adaptorUserId: loggedUser.Id);
        }

        var scheduler = HpcConnectionFramework.SchedulerAdapters.SchedulerFactory.GetInstance(jobSpecification.Cluster.SchedulerType)
            .CreateScheduler(jobSpecification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, loggedUser.Id, _expirioService, _expirioToken, _logger);

        try
        {
            return await scheduler.GetQuantumTaskResultAsync(jobSpecification.Cluster, taskInfo.ScheduledJobId, credentials, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
        }
        catch (QSchedulerApiException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new Exceptions.External.RequestedObjectDoesNotExistException("QSchedulerResourceNotFound", submittedTaskId);
            }
            if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                throw new Exceptions.External.InvalidRequestException("QSchedulerTaskNotInRunnableState");
            }
            throw;
        }
    }

    public async Task<System.IO.Stream> GetQuantumTaskArtifactAsync(long submittedTaskId, string artifactName, AdaptorUser loggedUser)
    {
        var taskInfo = await GetSubmittedTaskInfoByIdAsync(submittedTaskId, loggedUser, checkSharedJobInfoAccess: true);

        var jobInfo = await _unitOfWork.SubmittedJobInfoRepository.GetJobsQuery()
            .Include(j => j.Specification)
                .ThenInclude(s => s.Cluster)
            .Include(j => j.Project)
                .ThenInclude(p => p.ClusterProjects)
                    .ThenInclude(cp => cp.ClusterProjectCredentials)
            .FirstOrDefaultAsync(j => j.Tasks.Any(t => t.Id == submittedTaskId))
            ?? throw new Exceptions.External.RequestedObjectDoesNotExistException("NotExistingJobInfo", submittedTaskId);

        var jobSpecification = jobInfo.Specification;
        if (jobSpecification.Cluster?.SchedulerType != SchedulerType.QScheduler)
        {
            throw new Exceptions.External.InvalidRequestException("TaskNotOnQSchedulerCluster");
        }

        ClusterAuthenticationCredentials credentials = null;
        if (jobSpecification.Cluster.ConnectionProtocol != ClusterConnectionProtocol.Http &&
            jobSpecification.Cluster.ConnectionProtocol != ClusterConnectionProtocol.Https)
        {
            var clusterInfoLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            credentials = await clusterInfoLogic.GetNextAvailableUserCredentials(jobSpecification.Cluster.Id, jobInfo.Project.Id, requireIsInitialized: true, adaptorUserId: loggedUser.Id);
        }

        var scheduler = HpcConnectionFramework.SchedulerAdapters.SchedulerFactory.GetInstance(jobSpecification.Cluster.SchedulerType)
            .CreateScheduler(jobSpecification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, loggedUser.Id, _expirioService, _expirioToken, _logger);

        try
        {
            return await scheduler.GetQuantumTaskArtifactAsync(jobSpecification.Cluster, taskInfo.ScheduledJobId, artifactName, credentials, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
        }
        catch (QSchedulerApiException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new Exceptions.External.RequestedObjectDoesNotExistException("QSchedulerResourceNotFound", submittedTaskId);
            }
            if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                throw new Exceptions.External.InvalidRequestException("QSchedulerTaskNotInRunnableState");
            }
            throw;
        }
    }
}
