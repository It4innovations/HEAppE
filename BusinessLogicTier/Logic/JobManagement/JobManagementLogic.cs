using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.ClusterInformation;
using HEAppE.BusinessLogicTier.Logic.JobManagement.Validators;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.Comparers;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.Exceptions.External;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.Utils;
using log4net;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement;

internal class JobManagementLogic : IJobManagementLogic
{
    protected static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private readonly Dictionary<TaskSpecification, TaskSpecification> _extraLongTaskDecomposedDependency;
    private readonly object _lockCreateJobObj = new();
    private readonly object _lockSubmitJobObj = new();
    private readonly List<TaskSpecification> _tasksToAddToSpec;
    private readonly List<TaskSpecification> _tasksToDeleteFromSpec;
    protected IUnitOfWork _unitOfWork;
    protected ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;

    internal JobManagementLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        _unitOfWork = unitOfWork;
        _tasksToDeleteFromSpec = new List<TaskSpecification>();
        _tasksToAddToSpec = new List<TaskSpecification>();
        _extraLongTaskDecomposedDependency = new Dictionary<TaskSpecification, TaskSpecification>();
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
    }

    public async Task<SubmittedJobInfo> CreateJob(JobSpecification specification, AdaptorUser loggedUser,
        bool isExtraLong)
    {
        var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
        var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);

        await CompleteJobSpecification(specification, loggedUser, clusterLogic, userLogic);
        _logger.Info($"User {loggedUser.GetLogIdentification()} is creating a job specified as {specification}");

        foreach (var task in specification.Tasks)
        {
            if (isExtraLong) DecomposeExtraLongTask(task);
        }

        //delete and add extra long task specification divided to single tasks
        if (isExtraLong)
        {
            foreach (var task in _tasksToDeleteFromSpec) specification.Tasks.Remove(task);
            foreach (var task in _tasksToAddToSpec) specification.Tasks.Add(task);
        }

        var jobValidation = new JobManagementValidator(specification, _unitOfWork, _sshCertificateAuthorityService, _httpContextKeys).Validate();
        if (!jobValidation.IsValid)
            throw new InputValidationException("NotValidJobSpecification", jobValidation.Message);

        //lock (_lockCreateJobObj)
        {
            SubmittedJobInfo jobInfo;
            jobInfo = CreateSubmittedJobInfo(specification);
            /*using (var transactionScope = new TransactionScope(
                       TransactionScopeOption.Required,
                       new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                       TransactionScopeAsyncFlowOption.Enabled))
                       */
            {
                _unitOfWork.JobSpecificationRepository.Insert(specification);
                _unitOfWork.SubmittedJobInfoRepository.Insert(jobInfo);
                await _unitOfWork.SaveAsync();
                //transactionScope.Complete();
            }

            var clusterProject =
                _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(
                    jobInfo.Specification.ClusterId, jobInfo.Project.Id)
                ?? throw new InvalidRequestException("NotExistingProject");

            //Create job directory
            SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
                .CreateScheduler(specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id)
                .CreateJobDirectory(jobInfo, clusterProject.ScratchStoragePath, BusinessLogicConfiguration.SharedAccountsPoolMode, _httpContextKeys.Context.SshCaToken);
            return jobInfo;
        }
    }

    public virtual SubmittedJobInfo SubmitJob(long createdJobInfoId, AdaptorUser loggedUser)
    {
        _logger.Info($"User {loggedUser.GetLogIdentification()} is submitting the job with info Id {createdJobInfoId}");
        var jobInfo = GetSubmittedJobInfoById(createdJobInfoId, loggedUser);
        if(jobInfo.Specification.Tasks.Any(x=>x.CommandTemplate.IsEnabled == false))
            throw new InvalidRequestException("CannotSubmitJobWithDisabledCommandTemplate");
        
        if (jobInfo.State == JobState.Configuring || jobInfo.State == JobState.WaitingForServiceAccount)
        {
            if (!BusinessLogicConfiguration.SharedAccountsPoolMode)
                //Check if user is already running job - if yes set state to WaitingForUser - else run the job
                lock (_lockSubmitJobObj)
                {
                    var isJobUserAvailable = true;
                    var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
                    isJobUserAvailable = clusterLogic.IsUserAvailableToRun(jobInfo.Specification.ClusterUser);

                    if (!isJobUserAvailable)
                    {
                        jobInfo.State = JobState.WaitingForServiceAccount;
                        _unitOfWork.SubmittedJobInfoRepository.Update(jobInfo);
                        _unitOfWork.Save();
                        return jobInfo;
                    }
                }

            jobInfo.SubmitTime = DateTime.UtcNow;
            var submittedTasks = SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
                .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id)
                .SubmitJob(jobInfo.Specification, jobInfo.Specification.ClusterUser, _httpContextKeys.Context.SshCaToken);


            jobInfo = CombineSubmittedJobInfoFromCluster(jobInfo, submittedTasks);
            _unitOfWork.SubmittedJobInfoRepository.Update(jobInfo);
            _unitOfWork.Save();
            return jobInfo;
        }

        throw new InputValidationException("SubmittingJobNotInConfiguringState");
    }

    public async Task<SubmittedJobInfo> GetActualTasksInfo(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        _logger.Info($"User {loggedUser.GetLogIdentification()} is getting actual tasks info for the job with info Id {submittedJobInfoId}");
        var jobInfo = GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        var cluster = jobInfo.Specification.Cluster;
        var serviceAccount = await
            _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                jobInfo.Specification.ClusterId, jobInfo.Specification.ProjectId, requireIsInitialized: true, adaptorUserId: loggedUser.Id);

        var actualUnfinishedSchedulerTasksInfo = SchedulerFactory.GetInstance(cluster.SchedulerType)
            .CreateScheduler(cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id)
            .GetActualTasksInfo(jobInfo.Tasks.Where(w => !w.Specification.DependsOn.Any()).ToList(), serviceAccount, _httpContextKeys.Context.SshCaToken)
            .ToList();

        foreach (var task in jobInfo.Tasks)
        {
            var actualUnfinishedSchedulerTaskInfo = actualUnfinishedSchedulerTasksInfo
                .FirstOrDefault(w => w.ScheduledJobId == task.ScheduledJobId);
            if (actualUnfinishedSchedulerTaskInfo != null)
                CombineSubmittedTaskInfoFromCluster(task, actualUnfinishedSchedulerTaskInfo);
        }

        UpdateJobStateByTasks(jobInfo);
        _unitOfWork.SubmittedJobInfoRepository.Update(jobInfo);
        _unitOfWork.Save();
        return jobInfo;
    }

    public virtual async Task<SubmittedJobInfo> CancelJob(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        _logger.Info(
            $"User {loggedUser.GetLogIdentification()} is canceling the job with info Id {submittedJobInfoId}");
        var jobInfo = GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        if (jobInfo.State is >= JobState.Submitted and < JobState.Finished)
        {
            var submittedTask = jobInfo.Tasks.Where(w => !w.Specification.DependsOn.Any())
                .ToList();

            var scheduler = SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
                .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id);
            scheduler.CancelJob(submittedTask, "Job cancelled manually by the client.",
                jobInfo.Specification.ClusterUser, _httpContextKeys.Context.SshCaToken);

            var cluster = jobInfo.Specification.Cluster;
            var serviceAccount = await
                _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                    jobInfo.Specification.ClusterId, jobInfo.Specification.ProjectId, requireIsInitialized: true, adaptorUserId: loggedUser.Id);
            var actualUnfinishedSchedulerTasksInfo = scheduler.GetActualTasksInfo(submittedTask, serviceAccount, _httpContextKeys.Context.SshCaToken)
                .ToList();

            foreach (var task in jobInfo.Tasks)
            foreach (var actualUnfinishedSchedulerTaskInfo in actualUnfinishedSchedulerTasksInfo)
                CombineSubmittedTaskInfoFromCluster(task, actualUnfinishedSchedulerTaskInfo);

            UpdateJobStateByTasks(jobInfo);
            _unitOfWork.SubmittedJobInfoRepository.Update(jobInfo);
            _unitOfWork.Save();
        }
        else if (jobInfo.State is JobState.WaitingForServiceAccount || jobInfo.State is JobState.Configuring)
        {
            jobInfo.State = JobState.Canceled;
            jobInfo.Tasks.ForEach(f => f.State = TaskState.Canceled);
            _unitOfWork.SubmittedJobInfoRepository.Update(jobInfo);
        }
        else
        {
            throw new InvalidRequestException("CannotCancelJob", submittedJobInfoId, jobInfo.State);
        }

        return jobInfo;
    }

    public virtual bool DeleteJob(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        _logger.Info($"User {loggedUser.GetLogIdentification()} is deleting the job with info Id {submittedJobInfoId}");
        var jobInfo = GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        var clusterProject =
            _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(jobInfo.Specification.ClusterId,
                jobInfo.Project.Id) ?? throw new InvalidRequestException("NotExistingProject");

        if (jobInfo.State is JobState.Configuring
            or >= JobState.Finished and not JobState.WaitingForServiceAccount and not JobState.Deleted)
        {
            var isDeleted = SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
                .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id)
                .DeleteJobDirectory(jobInfo, clusterProject.ScratchStoragePath, _httpContextKeys.Context.SshCaToken);
            if (isDeleted)
            {
                jobInfo.State = JobState.Deleted;
                jobInfo.Tasks.ForEach(f => f.State = TaskState.Deleted);
                _unitOfWork.SubmittedJobInfoRepository.Update(jobInfo);
                _unitOfWork.Save();
            }

            return isDeleted;
        }

        throw new InvalidRequestException("CannotDeleteJob", submittedJobInfoId, jobInfo.State);
    }
    
    public virtual bool ArchiveJob(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        _logger.Info($"User {loggedUser.GetLogIdentification()} is archiving the job with info Id {submittedJobInfoId}");
        var jobInfo = GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        
        var basePath = jobInfo.Specification.Cluster.ClusterProjects
            .Find(cp => cp.ProjectId == jobInfo.Specification.ProjectId)?.ScratchStoragePath;
        var projectBasePath = jobInfo.Specification.Cluster.ClusterProjects
            .Find(cp => cp.ProjectId == jobInfo.Specification.ProjectId)?.ProjectStoragePath;
        if (string.IsNullOrEmpty(projectBasePath))
        {
            projectBasePath = basePath;
        }
        
        var localBasePath = Path.Combine(
                basePath, 
                HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath, 
                HPCConnectionFrameworkConfiguration.ScriptsSettings.SubExecutionsPath.TrimStart('/'),
                jobInfo.Specification.ClusterUser.Username);
        var jobLogArchivePath = Path.Combine(
                projectBasePath, 
                HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath, 
                HPCConnectionFrameworkConfiguration.ScriptsSettings.JobLogArchiveSubPath.TrimStart('/'), 
                jobInfo.Specification.ClusterUser.Username);
        
        var sourceDestinations = jobInfo.Specification.Tasks
            .SelectMany(x => new[]
            {
                CreatePathTuple(localBasePath, jobLogArchivePath, x, x.StandardOutputFile),
                CreatePathTuple(localBasePath, jobLogArchivePath, x, x.StandardErrorFile),
            });
        
        var isArchived = SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType).
            CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id).
            MoveJobFiles(jobInfo, sourceDestinations, _httpContextKeys.Context.SshCaToken);
        return isArchived;
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

    public virtual SubmittedJobInfo GetSubmittedJobInfoById(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        var jobInfo = _unitOfWork.SubmittedJobInfoRepository.GetByIdWithTasks(submittedJobInfoId)
                      ?? throw new RequestedObjectDoesNotExistException("NotExistingJobInfo", submittedJobInfoId);

        if (!LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys)
                .AuthorizeUserForJobInfo(loggedUser, jobInfo))
            throw new AdaptorUserNotAuthorizedForJobException("UserNotAuthorizedToWorkWithJob",
                loggedUser.GetLogIdentification(), submittedJobInfoId);
        return jobInfo;
    }


    public virtual SubmittedTaskInfo GetSubmittedTaskInfoById(long submittedTaskInfoId, AdaptorUser loggedUser)
    {
        var taskInfo = _unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskInfoId)
                       ?? throw new RequestedObjectDoesNotExistException("NotExistingTaskInfo", submittedTaskInfoId);

        if (!LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys)
                .AuthorizeUserForTaskInfo(loggedUser, taskInfo))
            throw new AdaptorUserNotAuthorizedForJobException("UserNotAuthorizedToWorkWithTask",
                loggedUser.GetLogIdentification(), submittedTaskInfoId);
        return taskInfo;
    }

    public virtual IEnumerable<SubmittedJobInfo> GetJobsForUser(AdaptorUser loggedUser)
    {
        return _unitOfWork.SubmittedJobInfoRepository.GetAllForSubmitterId(loggedUser.Id);
    }

    public virtual IEnumerable<SubmittedJobInfo> GetNotFinishedJobInfosForSubmitterId(long submitterId)
    {
        return _unitOfWork.SubmittedJobInfoRepository.GetNotFinishedForSubmitterId(submitterId);
    }

    public virtual IEnumerable<SubmittedJobInfo> GetNotFinishedJobInfos()
    {
        return _unitOfWork.SubmittedJobInfoRepository.GetAllUnfinished();
    }

    public IEnumerable<SubmittedTaskInfo> GetAllFinishedTaskInfos(IEnumerable<long> taskIds)
    {
        return _unitOfWork.SubmittedTaskInfoRepository.GetAllFinished().Where(w => taskIds.Contains(w.Id))
            .ToList();
    }

    /// <summary>
    ///     Updates jobs in db with received info from HPC schedulers
    /// </summary>
    public async Task UpdateCurrentStateOfUnfinishedJobs()
    {
        var jobsGroup = _unitOfWork.SubmittedJobInfoRepository.GetAllUnfinished()
            .GroupBy(g => new { g.Specification.Cluster, g.Project })
            .ToList();

        foreach (var jobGroup in jobsGroup)
        {
            var cluster = jobGroup.Key.Cluster;
            var project = jobGroup.Key.Project;
            _logger.Info($"Updating current state of unfinished jobs for cluster {cluster.Name} and project {project.Name}");
            
            var actualUnfinishedSchedulerTasksInfo = new List<SubmittedTaskInfo>();

            var userJobsGroup = jobGroup.GroupBy(g => g.Specification.ClusterUser)
                .ToList();

            foreach (var userJobGroup in userJobsGroup)
            {
                var tasksExceedWaitLimit = userJobGroup.Where(w => IsWaitingLimitExceeded(w))
                    .SelectMany(s => s.Tasks)
                    .Where(w => !w.Specification.DependsOn.Any())
                    .ToList();

                if (tasksExceedWaitLimit.Any())
                {
                    SchedulerFactory
                        .GetInstance(cluster.SchedulerType)
                        .CreateScheduler(cluster, project, _sshCertificateAuthorityService, adaptorUserId: userJobGroup.First().Submitter.Id)
                        .CancelJob(tasksExceedWaitLimit, "Job cancelled automatically by exceeding waiting limit.", userJobGroup.Key, _httpContextKeys.Context.SshCaToken);
                    tasksExceedWaitLimit.ForEach(x=>_logger.Warn($"Job {x.ScheduledJobId} was cancelled because it exceeded waiting limit."));
                }
            }

            IRexScheduler scheduler = !project.IsOneToOneMapping ?
                scheduler = SchedulerFactory
                    .GetInstance(cluster.SchedulerType)
                    .CreateScheduler(cluster, project, _sshCertificateAuthorityService, null) : null;

            Func<long, IRexScheduler> schedulerProxy = (long adaptorUserId) => scheduler != null ? scheduler : SchedulerFactory
                .GetInstance(cluster.SchedulerType)
                .CreateScheduler(cluster, project, _sshCertificateAuthorityService, adaptorUserId: adaptorUserId);

            if (cluster.UpdateJobStateByServiceAccount.Value)
                actualUnfinishedSchedulerTasksInfo =
                    (await GetActualTasksStateInHPCScheduler(_unitOfWork, schedulerProxy, jobGroup.SelectMany(s => s.Tasks), true))
                        .ToList();
            else
                userJobsGroup.ForEach(async f =>
                    actualUnfinishedSchedulerTasksInfo.AddRange(
                        await GetActualTasksStateInHPCScheduler(_unitOfWork, schedulerProxy, f.SelectMany(s => s.Tasks), false)));

            var isNeedUpdateJobState = false;
            foreach (var submittedJob in jobGroup)
            {
                foreach (var submittedTask in submittedJob.Tasks)
                {
                    var actualUnfinishedSchedulerTaskInfo =
                        actualUnfinishedSchedulerTasksInfo.FirstOrDefault(w =>
                            w.ScheduledJobId == submittedTask.ScheduledJobId);
                    if (actualUnfinishedSchedulerTaskInfo is null)
                    {
                        // Failed job which is not returned from schedulers
                        submittedTask.State = TaskState.Failed;
                        isNeedUpdateJobState = true;
                    }
                    else if (submittedTask.State != actualUnfinishedSchedulerTaskInfo.State)
                    {
                        var submittedTaskInfo =
                            CombineSubmittedTaskInfoFromCluster(submittedTask, actualUnfinishedSchedulerTaskInfo);
                        isNeedUpdateJobState = true;
                    }
                }

                if (isNeedUpdateJobState)
                {
                    UpdateJobStateByTasks(submittedJob);
                    _unitOfWork.SubmittedJobInfoRepository.Update(submittedJob);
                    isNeedUpdateJobState = false;
                }
            }
        }

        _unitOfWork.Save();
    }

    public void CopyJobDataToTemp(long createdJobInfoId, AdaptorUser loggedUser, string hash, string path)
    {
        _logger.Info(string.Format("User {0} with job Id {1} is copying job data to temp {2}",
            loggedUser.GetLogIdentification(), createdJobInfoId, hash));
        var jobInfo = GetSubmittedJobInfoById(createdJobInfoId, loggedUser);
        var clusterProject =
            _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(jobInfo.Specification.ClusterId,
                jobInfo.Project.Id)
            ?? throw new InvalidRequestException("NotExistingProject");

        SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
            .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id)
            .CopyJobDataToTemp(jobInfo, clusterProject.ScratchStoragePath, hash, path, _httpContextKeys.Context.SshCaToken);
    }


    public void CopyJobDataFromTemp(long createdJobInfoId, AdaptorUser loggedUser, string hash)
    {
        _logger.Info(string.Format("User {0} with job Id {1} is copying job data from temp {2}",
            loggedUser.GetLogIdentification(), createdJobInfoId, hash));
        var jobInfo = GetSubmittedJobInfoById(createdJobInfoId, loggedUser);
        var clusterProject =
            _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(jobInfo.Specification.ClusterId,
                jobInfo.Project.Id)
            ?? throw new InvalidRequestException("NotExistingProject");

        SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
            .CreateScheduler(jobInfo.Specification.Cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id)
            .CopyJobDataFromTemp(jobInfo, clusterProject.ScratchStoragePath, hash, _httpContextKeys.Context.SshCaToken);
    }

    public IEnumerable<string> GetAllocatedNodesIPs(long submittedTaskInfoId, AdaptorUser loggedUser)
    {
        var taskInfo = GetSubmittedTaskInfoById(submittedTaskInfoId, loggedUser);
        if (taskInfo.State != TaskState.Running)
            throw new InputValidationException("IPAddressesProvidedOnlyForRunningTask");

        var cluster = taskInfo.Specification.JobSpecification.Cluster;
        var stringIPs = SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, taskInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id)
            .GetAllocatedNodes(taskInfo, _httpContextKeys.Context.SshCaToken);
        return stringIPs;
    }

    public async Task<DryRunJobInfo> DryRunJob(long modelProjectId, long modelClusterNodeTypeId, long modelNodes,
        long modelTasksPerNode,
        long modelWallTimeInMinutes, AdaptorUser loggedUser)
    {
        var project = _unitOfWork.ProjectRepository.GetByIdWithClusterProjects(modelProjectId)
                      ?? throw new RequestedObjectDoesNotExistException("NotExistingProject", modelProjectId);
        var clusterNodeType = _unitOfWork.ClusterNodeTypeRepository.GetById(modelClusterNodeTypeId)
                              ?? throw new RequestedObjectDoesNotExistException("NotExistingClusterNodeType",
                                  modelClusterNodeTypeId);
        var cluster = clusterNodeType.Cluster;

        var dryRunJobSpecification = new DryRunJobSpecification
        {
            Project = project,
            ClusterNodeType = clusterNodeType,
            Nodes = modelNodes,
            TasksPerNode = modelTasksPerNode,
            WallTimeInMinutes = modelWallTimeInMinutes,
            ClusterUser = await LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys)
                .GetNextAvailableUserCredentials(cluster.Id, project.Id, requireIsInitialized: true, adaptorUserId: loggedUser.Id)
        };
        
        return SchedulerFactory.GetInstance(cluster.SchedulerType)
            .CreateScheduler(cluster, project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id)
            .DryRunJob(dryRunJobSpecification, _httpContextKeys.Context.SshCaToken);
    }

    public IQueryable<SubmittedJobInfo> GetJobsForUserQuery(long loggedUserId)
    {
        return _unitOfWork.SubmittedJobInfoRepository.GetJobsForUserQuery(loggedUserId);
    }

   protected async Task CompleteJobSpecification(JobSpecification specification, AdaptorUser loggedUser,
    IClusterInformationLogic clusterLogic, IUserAndLimitationManagementLogic userLogic)
    {
        // 1. Initial job-level data retrieval
        var cluster = clusterLogic.GetClusterById(specification.ClusterId);
        specification.Cluster = cluster;
        specification.Submitter = loggedUser;
        
        // Fetch Project and SubProject
        specification.Project = _unitOfWork.ProjectRepository.GetById(specification.ProjectId);
        if (specification.SubProjectId.HasValue)
        {
            specification.SubProject = _unitOfWork.SubProjectRepository.GetById(specification.SubProjectId.Value);
        }

        // Default group assignment
        specification.SubmitterGroup ??= userLogic.GetDefaultSubmitterGroup(loggedUser, specification.ProjectId);

        // 2. File transfer logic initialization
        specification.FileTransferMethod = LogicFactory.GetLogicFactory()
            .CreateFileTransferLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys)
            .GetFileTransferMethodsByClusterId(cluster.Id)
            .FirstOrDefault(f => f.Id == specification.FileTransferMethodId.Value);

        // 3. PERFORMANCE: Manual batch fetch using GetById
        // Since .Where() is not available, we fetch unique templates before the main loop
        var templateIds = specification.Tasks
            .Select(t => t.CommandTemplateId)
            .Distinct()
            .ToList();

        var templates = new Dictionary<long, CommandTemplate>();
        foreach (var id in templateIds)
        {
            var template = _unitOfWork.CommandTemplateRepository.GetById(id);
            if (template != null)
            {
                templates[id] = template;
            }
        }

        // 4. Async fetch for credentials
        specification.ClusterUser = await clusterLogic.GetNextAvailableUserCredentials(
            cluster.Id, 
            specification.ProjectId, 
            requireIsInitialized: true, 
            adaptorUserId: loggedUser.Id);

        // 5. Optimized Task iteration
        foreach (var task in specification.Tasks)
        {
            // Use the pre-fetched dictionary for O(1) template lookup
            if (templates.TryGetValue(task.CommandTemplateId, out var commandTemplate) && commandTemplate.IsGeneric)
            {
                // PERFORMANCE: Use .ToHashSet() to resolve ambiguous constructor reference
                var definedGenericIds = commandTemplate.TemplateParameters
                    .Select(x => x.Identifier)
                    .ToHashSet();

                // PERFORMANCE: Single pass filtering to separate parameters (reduces allocations)
                var userDefinedParams = new List<CommandTemplateParameterValue>();
                CommandTemplateParameterValue userScriptParam = null;

                foreach (var val in task.CommandParameterValues)
                {
                    if (definedGenericIds.Contains(val.CommandParameterIdentifier))
                    {
                        userScriptParam ??= val;
                    }
                    else
                    {
                        userDefinedParams.Add(val);
                    }
                }

                if (userScriptParam != null)
                {
                    // Find the target parameter name that isn't the script parameter itself
                    var targetParamName = commandTemplate.TemplateParameters
                        .FirstOrDefault(x => x.Identifier != userScriptParam.CommandParameterIdentifier)?.Identifier;

                    if (targetParamName != null)
                    {
                        var parsedValue = AddGenericCommandUserDefinedCommands(userDefinedParams);

                        // Clean up and add the parsed aggregate parameter
                        task.CommandParameterValues.RemoveAll(x => !definedGenericIds.Contains(x.CommandParameterIdentifier));
                        
                        task.CommandParameterValues.Add(new CommandTemplateParameterValue
                        {
                            CommandParameterIdentifier = targetParamName,
                            Value = parsedValue
                        });
                    }
                }
            }

            // Complete individual task logic
            CompleteTaskSpecification(task, clusterLogic);
            
            // Merge environment variables
            task.EnvironmentVariables = CombineJobAndTaskEnvironmentVariables(
                    specification.EnvironmentVariables, 
                    task.EnvironmentVariables)
                .ToList();
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

        foreach (var cmdParameterValue in
                 taskSpecification.CommandParameterValues ?? Enumerable.Empty<CommandTemplateParameterValue>())
            cmdParameterValue.TemplateParameter =
                _unitOfWork.CommandTemplateParameterRepository.GetByCommandTemplateIdAndCommandParamId(
                    taskSpecification.CommandTemplateId, cmdParameterValue.CommandParameterIdentifier);

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
        // Performance: If the number of tasks is large, pre-calculating count 
        // and using a direct projection is faster than multiple LINQ iterations.
        var taskCount = specification.Tasks.Count;
    
        var result = new SubmittedJobInfo
        {
            CreationTime = DateTime.UtcNow,
            Name = specification.Name,
            Project = specification.Project,
            Specification = specification,
            State = JobState.Configuring,
            Submitter = specification.Submitter,
        
            // Performance: Removed OrderByDescending unless strictly necessary for business logic.
            // If ordering is needed, do it once at the end or on the UI layer.
            Tasks = specification.Tasks
                .Select(s => new SubmittedTaskInfo
                {
                    Name = s.Name,
                    Specification = s,
                    State = TaskState.Configuring,
                    // Performance: Using Null-coalescing operator is efficient here
                    Priority = s.Priority ?? TaskPriority.Average,
                    NodeType = s.ClusterNodeType,
                    Project = s.Project
                })
                .ToList() // ToList() on a Select expression is highly optimized in modern .NET
        };

        return result;
    }

    protected static void UpdateJobStateByTasks(SubmittedJobInfo dbJobInfo)
    {
        dbJobInfo.StartTime = dbJobInfo.Tasks.FirstOrDefault()?.StartTime;
        dbJobInfo.EndTime = dbJobInfo.Tasks.Where(t => t.EndTime.HasValue).LastOrDefault()?.EndTime;
        dbJobInfo.TotalAllocatedTime = dbJobInfo.Tasks.Sum(s => s.AllocatedTime ?? 0);

        var continuousJobState = JobState.Finished;
        var minTaskState = TaskState.Canceled;
        foreach (var task in dbJobInfo.Tasks)
        {
            minTaskState = task.State < minTaskState ? task.State : minTaskState;
            if (task.State == TaskState.Failed)
            {
                continuousJobState = JobState.Failed;
                break;
            }

            if (task.State == TaskState.Running)
            {
                continuousJobState = JobState.Running;
                break;
            }

            if (task.State == TaskState.Canceled)
            {
                continuousJobState = JobState.Canceled;
                break;
            }
        }

        dbJobInfo.State = (JobState)minTaskState < continuousJobState ? (JobState)minTaskState : continuousJobState;
    }

    protected static SubmittedJobInfo CombineSubmittedJobInfoFromCluster(SubmittedJobInfo dbJobInfo,
        IEnumerable<SubmittedTaskInfo> submittedTasksInfo)
    {
        try
        {
            dbJobInfo.Tasks.ForEach(s =>
                CombineSubmittedTaskInfoFromCluster(s, submittedTasksInfo.First(f => f.Name == s.Specification.Id.ToString())));
        }
        catch (Exception ex)
        {
            _logger.Error("Error combining submitted job info from cluster: " + ex.Message);
            throw new InvalidRequestException("ErrorCombiningJobInfoFromCluster", dbJobInfo.Id);
        }
        

        UpdateJobStateByTasks(dbJobInfo);
        return dbJobInfo;
    }

    private static async Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksStateInHPCScheduler(IUnitOfWork unitOfWork,
        Func<long, IRexScheduler> scheduler, IEnumerable<SubmittedTaskInfo> jobTasks, bool useServiceAccount)
    {
        var unfinishedTasks = jobTasks
            .Where(w => w.State is > TaskState.Configuring and (<= TaskState.Running or TaskState.Canceled))
            .ToList();

        var jobSpecification = unfinishedTasks.FirstOrDefault().Specification.JobSpecification;

        var account = useServiceAccount
            ? await unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                jobSpecification.ClusterId, jobSpecification.ProjectId, requireIsInitialized: true, adaptorUserId: jobSpecification.Submitter.Id)
            : jobSpecification.ClusterUser;
        _logger.Info($"Getting actual tasks state for job {jobSpecification.Id} using account {account.Username}");
        return scheduler(jobSpecification.Submitter.Id).GetActualTasksInfo(unfinishedTasks, account, null);
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

    protected static SubmittedTaskInfo CombineSubmittedTaskInfoFromCluster(SubmittedTaskInfo dbTaskInfo,
        SubmittedTaskInfo clusterTaskInfo)
    {
        ResourceAccountingUtils.ComputeAccounting(dbTaskInfo, clusterTaskInfo, _logger);

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
        dbTaskInfo.State = clusterTaskInfo.State;
        dbTaskInfo.AllParameters = clusterTaskInfo.AllParameters;
        dbTaskInfo.ErrorMessage = clusterTaskInfo.ErrorMessage;
        dbTaskInfo.Reason = clusterTaskInfo.Reason;
        return dbTaskInfo;
    }
}