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
using HEAppE.Exceptions.External;
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
    internal JobManagementLogic(IUnitOfWork unitOfWork, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, 
                                IHttpContextKeys httpContextKeys, IExpirioService expirioService, ILogger logger)
    {
        using var serviceScope = ServiceActivator.GetScope();
        _unitOfWork = unitOfWork;
        _tasksToDeleteFromSpec = new List<TaskSpecification>();
        _tasksToAddToSpec = new List<TaskSpecification>();
        _extraLongTaskDecomposedDependency = new Dictionary<TaskSpecification, TaskSpecification>();
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _userOrgService = userOrgService;
        _logger = logger;
        _expirioService = expirioService;
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
        var (jobInfo, credentials) = await PrepareGetActualTasksInfoAsync(submittedJobInfoId, loggedUser);
        
        var actualUnfinishedSchedulerTasksInfo = await SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
            .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
            .GetActualTasksInfoAsync(jobInfo.Tasks.Where(w => !w.Specification.DependsOn.Any()).ToList(), credentials, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

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

        var serviceAccountsCache = new Dictionary<(long ClusterId, long ProjectId), ClusterAuthenticationCredentials>();
        foreach (var job in allUnfinishedJobs)
        {
            var cluster = job.Specification.Cluster;
            if (cluster.UpdateJobStateByServiceAccount.Value)
            {
                var key = (job.Specification.ClusterId, job.Specification.ProjectId);
                if (!serviceAccountsCache.ContainsKey(key))
                {
                    var account = await _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                        key.ClusterId, key.ProjectId, requireIsInitialized: true, adaptorUserId: null, logger: _logger);
                    if (account != null)
                    {
                        serviceAccountsCache[key] = account;
                    }
                }
            }
        }

        var jobTaskDataList = allUnfinishedJobs.Select(job => new
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
                ClusterUsername = j.Job.Specification.Cluster.UpdateJobStateByServiceAccount.Value 
                    ? string.Empty 
                    : j.Job.Specification.ClusterUser.Username
            })
            .ToList();

        foreach (var group in hpcQueryGroups)
        {
            var itemsInGroup = group.ToList();
            var firstItem = itemsInGroup.First();

            var cluster = firstItem.Job.Specification.Cluster;
            var clusterUser = cluster.UpdateJobStateByServiceAccount.Value 
                ? null 
                : firstItem.Job.Specification.ClusterUser;

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

                _logger.LogInformation($"Requesting state for a bulk batch of {tasksList.Count} tasks on cluster {cluster.Name} using account {account?.Username}");
                
                var states = await scheduler.GetActualTasksInfoAsync(tasksList, account, null, null);
                if (states != null)
                {
                    groupTasksResult.AddRange(states);
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

                    bool isNeedUpdateJobState = false;
                    foreach (var submittedTask in submittedJob.Tasks)
                    {
                        // Skip tasks that were not queried (e.g. Finished, Failed, or Configuring)
                        if (!(submittedTask.State > TaskState.Configuring && (submittedTask.State <= TaskState.Running || submittedTask.State == TaskState.Canceled)))
                        {
                            continue;
                        }
                       
                        var actualUnfinishedSchedulerTaskInfo = groupTasksResult.FirstOrDefault(w => w.ScheduledJobId == submittedTask.ScheduledJobId);
                        if (actualUnfinishedSchedulerTaskInfo is null)
                        {
                            if (submittedTask.State is > TaskState.Configuring and (<= TaskState.Running or TaskState.Canceled))
                            {
                                submittedTask.State = TaskState.Failed;
                                isNeedUpdateJobState = true;
                            }
                        }
                        else if (submittedTask.State != actualUnfinishedSchedulerTaskInfo.State)
                        {
                            CombineSubmittedTaskInfoFromCluster(submittedTask, actualUnfinishedSchedulerTaskInfo);
                            isNeedUpdateJobState = true;
                        }
                    }

                    var jobStateChanged = UpdateJobStateByTasks(submittedJob);
                    if (isNeedUpdateJobState || jobStateChanged)
                    {
                        // UpdateJobStateByTasks(submittedJob);
                        _unitOfWork.SubmittedJobInfoRepository.Update(submittedJob); // TODO: check
                        JobCacheManager.InvalidateJobCache(submittedJob.Id);
                    }
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
                var commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(task.CommandTemplateId);
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
        taskSpecification.CommandTemplate =
            _unitOfWork.CommandTemplateRepository.GetById(taskSpecification.CommandTemplateId);

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

    protected static bool UpdateJobStateByTasks(SubmittedJobInfo dbJobInfo)
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
        dbTaskInfo.State = clusterTaskInfo.State;
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
                var commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId)
                                      ?? throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound", commandTemplateId);

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
        
        var credentials = await clusterLogic.GetNextAvailableUserCredentials(
            specification.ClusterId, specification.ProjectId, requireIsInitialized: true, adaptorUserId: loggedUser.Id);
        CompleteJobSpecification(specification, loggedUser, clusterLogic, userLogic, credentials);
        _logger.LogInformation($"User {loggedUser.GetLogIdentification()} is creating a job specified as {specification}");

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
        
        {
            SubmittedJobInfo jobInfo;
            jobInfo = CreateSubmittedJobInfo(specification);
            using (var transactionScope = new TransactionScope(
                       TransactionScopeOption.Required,
                       new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                       TransactionScopeAsyncFlowOption.Enabled))
            {
                _unitOfWork.JobSpecificationRepository.Insert(specification);
                _unitOfWork.SubmittedJobInfoRepository.Insert(jobInfo);

                await _unitOfWork.SaveAsync();
                transactionScope.Complete();
            }

            return jobInfo;
        }
    }

    public async Task DeleteJobDbRecord(long jobInfoId, long specificationId)
    {
        using (var cleanupTransaction = new TransactionScope(
                   TransactionScopeOption.Required,
                   new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                   TransactionScopeAsyncFlowOption.Enabled))
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
            cleanupTransaction.Complete();
        }
    }

    public async Task<(SubmittedJobInfo JobInfo, bool IsWaitingForServiceAccount)> PrepareJobForSubmitAsync(long createdJobInfoId, AdaptorUser loggedUser)
    {
        var jobInfo = await GetSubmittedJobInfoByIdAsync(createdJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);
        if (jobInfo.Specification.Tasks.Any(x => x.CommandTemplate.IsEnabled == false))
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

        return (jobInfo, false);
    }

    public async Task<SubmittedJobInfo> CompleteJobSubmitAsync(long createdJobInfoId, AdaptorUser loggedUser, IEnumerable<SubmittedTaskInfo> submittedTasks)
    {
        var jobInfo = await GetSubmittedJobInfoByIdAsync(createdJobInfoId, loggedUser);
        jobInfo.SubmitTime = DateTime.UtcNow;
        jobInfo = CombineSubmittedJobInfoFromCluster(jobInfo, submittedTasks);
        await _unitOfWork.SaveAsync();
        return jobInfo;
    }

    public async Task<(SubmittedJobInfo JobInfo, ClusterAuthenticationCredentials Credentials)> PrepareGetActualTasksInfoAsync(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        var jobInfo = await GetSubmittedJobInfoByIdAsync(submittedJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);

        // Kerberos clusters (e.g. Metacentrum) have no persistent service account.
        // The per-user ClusterUser is the correct credential; Kerberos ticket is obtained
        // from the user's active SSH/PBS session context, not from a stored secret.
        if (jobInfo.Specification.ClusterUser?.AuthenticationType == ClusterAuthenticationCredentialsAuthType.Kerberos)
        {
            return (jobInfo, jobInfo.Specification.ClusterUser);
        }

        var credentials = await
            _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                jobInfo.Specification.ClusterId, jobInfo.Specification.ProjectId, requireIsInitialized: true, adaptorUserId: loggedUser.Id, _logger);
        return (jobInfo, credentials);
    }

    public async Task<SubmittedJobInfo> CompleteGetActualTasksInfoAsync(long submittedJobInfoId, AdaptorUser loggedUser, IEnumerable<SubmittedTaskInfo> actualTasksInfo)
    {
        var jobInfo = await GetSubmittedJobInfoByIdAsync(submittedJobInfoId, loggedUser);
        var actualUnfinishedSchedulerTasksInfo = actualTasksInfo.ToList();

        foreach (var task in jobInfo.Tasks)
        {
            var actualUnfinishedSchedulerTaskInfo = actualUnfinishedSchedulerTasksInfo
                .FirstOrDefault(w => w.ScheduledJobId == task.ScheduledJobId);
            if (actualUnfinishedSchedulerTaskInfo != null)
                CombineSubmittedTaskInfoFromCluster(task, actualUnfinishedSchedulerTaskInfo);
        }

        UpdateJobStateByTasks(jobInfo);
        await _unitOfWork.SaveAsync();
        return jobInfo;
    }

    public async Task<(SubmittedJobInfo JobInfo, ClusterAuthenticationCredentials Credentials, bool CancelledLocally)> PrepareCancelJobAsync(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        var jobInfo = await GetSubmittedJobInfoByIdAsync(submittedJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);
        if (jobInfo.State is >= JobState.Submitted and < JobState.Finished)
        {
            var credentials = await
                _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                    jobInfo.Specification.ClusterId, jobInfo.Specification.ProjectId, requireIsInitialized: true, adaptorUserId: loggedUser.Id, logger: _logger);
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
        // 1. Find all potential candidate tasks matching this scheduledJobId
        var candidates = await _unitOfWork.SubmittedTaskInfoRepository.GetTasksByScheduledJobIdAsync(scheduledJobId);
        if (candidates == null || !candidates.Any())
        {
            throw new KeyNotFoundException($"Task with scheduler ID {scheduledJobId} not found.");
        }

        SubmittedTaskInfo? dbTask = null;
        SubmittedJobInfo? jobInfo = null;
        Cluster? cluster = null;

        foreach (var candidate in candidates)
        {
            var job = await _unitOfWork.SubmittedJobInfoRepository.GetByIdWithTasksAsync(candidate.Specification.JobSpecification.Id);
            if (job == null) continue;

            var currentCluster = job.Specification.Cluster;
            bool isAuthenticated = false;

            if (currentCluster.SchedulerType == SchedulerType.QScheduler)
            {
                bool toggled = currentCluster.CustomConfigurationVaultToggles != null &&
                               currentCluster.CustomConfigurationVaultToggles.TryGetValue("QSchedulerNotifyToken", out bool toggledValue) &&
                               toggledValue;
                bool storeInVault = toggled;

                string? expectedToken = null;
                if (storeInVault)
                {
                    var vaultConnector = new VaultConnector(_logger);
                    expectedToken = await vaultConnector.GetClusterSecretAsync(currentCluster.Id, "QSchedulerNotifyToken");
                }
                else
                {
                    if (currentCluster.CustomConfiguration != null && currentCluster.CustomConfiguration.TryGetValue("QSchedulerNotifyToken", out expectedToken))
                    {
                        // expectedToken populated
                    }
                }

                if (!string.IsNullOrEmpty(expectedToken) && expectedToken == token)
                {
                    isAuthenticated = true;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(candidate.CallbackSecret) && candidate.CallbackSecret == token)
                {
                    isAuthenticated = true;
                }
            }

            if (isAuthenticated)
            {
                dbTask = candidate;
                jobInfo = job;
                cluster = currentCluster;
                break;
            }
        }

        if (dbTask == null || jobInfo == null || cluster == null)
        {
            throw new UnauthorizedAccessException("Authentication failed: Invalid callback token.");
        }

        // 3. Prepare rawPayload
        string? rawPayload = rawResponse;
        if (cluster.SchedulerType == SchedulerType.QScheduler && !string.IsNullOrEmpty(qSchedulerState))
        {
            rawPayload = $"{{\"state\":\"{qSchedulerState.ToLower()}\"}}";
        }

        if (string.IsNullOrEmpty(rawPayload))
        {
            throw new ArgumentException("Callback raw response is empty.");
        }

        // If the task belongs to a QScheduler session and we receive the session "open" event,
        // trigger the actual task submission to that session.
        if (cluster.SchedulerType == SchedulerType.QScheduler && 
            dbTask.ScheduledJobId.StartsWith("session:") && 
            string.Equals(qSchedulerState, "open", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation($"Callback event 'open' received for QScheduler session '{dbTask.ScheduledJobId}'. Triggering task submission.");
            
            ClusterAuthenticationCredentials credentials;
            if (jobInfo.Specification.ClusterUser?.AuthenticationType == ClusterAuthenticationCredentialsAuthType.Kerberos)
            {
                credentials = jobInfo.Specification.ClusterUser;
            }
            else
            {
                credentials = await _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                    jobInfo.Specification.ClusterId, jobInfo.Specification.ProjectId, requireIsInitialized: true, adaptorUserId: dbTask.Specification.JobSpecification.Submitter.Id, _logger);
            }

            var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType)
                .CreateScheduler(cluster, jobInfo.Project, _sshCertificateAuthorityService, dbTask.Specification.JobSpecification.Submitter.Id, _expirioService, _expirioToken, _logger);
            
            var tasksToUpdate = jobInfo.Tasks.Where(t => t.ScheduledJobId == dbTask.ScheduledJobId).ToList();
            var updatedTasks = await scheduler.GetActualTasksInfoAsync(tasksToUpdate, credentials, "ForceSessionSubmit", _expirioToken);
            
            foreach (var updated in updatedTasks)
            {
                var t = jobInfo.Tasks.FirstOrDefault(x => x.Id == updated.Id);
                if (t != null)
                {
                    t.ScheduledJobId = updated.ScheduledJobId;
                    t.State = updated.State;
                    t.ErrorMessage = updated.ErrorMessage;
                }
            }
            
            UpdateJobStateByTasks(jobInfo);
            await _unitOfWork.SaveAsync();
            JobCacheManager.InvalidateJobCache(jobInfo.Id);
            return jobInfo.Id;
        }

        // 4. Parse state via converter
        var convertor = SchedulerFactory.GetInstance(cluster.SchedulerType).GetDataConvertor(_logger);
        var parsedTasks = convertor.ReadParametersFromResponse(cluster, rawPayload);
        var parsedTaskInfo = parsedTasks?.FirstOrDefault();

        if (parsedTaskInfo == null)
        {
            throw new Exception("Failed to parse callback payload using the cluster data convertor.");
        }

        // 5. Update task state in DB and aggregate job status
        if (parsedTaskInfo.State != TaskState.Unknown && dbTask.State < TaskState.Finished)
        {
            dbTask.State = parsedTaskInfo.State;
            dbTask.ErrorMessage = parsedTaskInfo.ErrorMessage;
            dbTask.EndTime = parsedTaskInfo.EndTime ?? DateTime.UtcNow;

            UpdateJobStateByTasks(jobInfo);

            await _unitOfWork.SaveAsync();
            JobCacheManager.InvalidateJobCache(jobInfo.Id);
        }

        return jobInfo.Id;
    }

#pragma warning disable IDE1006
    private string _expirioToken
    {
        get => !string.IsNullOrEmpty(_httpContextKeys.Context.LEXISToken) ? _httpContextKeys.Context.LEXISToken : _httpContextKeys.Context.IdpToken;
    }
#pragma warning restore IDE1006
}
