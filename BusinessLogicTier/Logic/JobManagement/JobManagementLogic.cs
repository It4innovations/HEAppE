using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.ClusterInformation;
using HEAppE.BusinessLogicTier.Logic.JobManagement.Exceptions;
using HEAppE.BusinessLogicTier.Logic.JobManagement.Validators;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement.Exceptions;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.Comparers;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.Utils.Validation;
using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Transactions;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement
{
    internal class JobManagementLogic : IJobManagementLogic
    {
        private readonly object _lockCreateJobObj = new();
        private readonly object _lockSubmitJobObj = new();

        protected static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected IUnitOfWork _unitOfWork;
        private readonly List<TaskSpecification> _tasksToDeleteFromSpec;
        private readonly List<TaskSpecification> _tasksToAddToSpec;
        private readonly Dictionary<TaskSpecification, TaskSpecification> _extraLongTaskDecomposedDependency;

        internal JobManagementLogic(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _tasksToDeleteFromSpec = new List<TaskSpecification>();
            _tasksToAddToSpec = new List<TaskSpecification>();
            _extraLongTaskDecomposedDependency = new Dictionary<TaskSpecification, TaskSpecification>();
        }

        public SubmittedJobInfo CreateJob(JobSpecification specification, AdaptorUser loggedUser, bool isExtraLong)
        {
            IUserAndLimitationManagementLogic userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork);
            IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork);

            CompleteJobSpecification(specification, loggedUser, clusterLogic, userLogic);
            _logger.Info($"User {loggedUser.GetLogIdentification()} is creating a job specified as {specification}");


            foreach (var task in specification.Tasks)
            {
                ResourceUsage currentUsage = userLogic.GetCurrentUsageAndLimitationsForUser(loggedUser)
                                                            .Where(w => w.NodeType.Id == task.ClusterNodeType.Id)
                                                            .FirstOrDefault();

                if (currentUsage == null)
                {
                    var message = $"Current usage for user {loggedUser.GetLogIdentification()} and node type " +
                                  $"{task.ClusterNodeType} was not created by the GetCurrentUsageAndLimitationsForUser method.";
                    _logger.Error(message);
                    throw new NotImplementedException(message);
                }
                else if (!CheckRequestedResourcesAgainstLimitations(task, currentUsage))
                {
                    var message = $"Requested resources for job {task.Name} exceeded user limitations.";
                    _logger.Error(message);
                    throw new RequestedJobResourcesExceededUserLimitationsException(message);
                }

                if (isExtraLong)
                {
                    DecomposeExtraLongTask(task);
                }
            }

            //delete and add extra long task specification divided to single tasks
            if (isExtraLong)
            {
                foreach (var task in _tasksToDeleteFromSpec)
                {
                    specification.Tasks.Remove(task);
                }
                foreach (var task in _tasksToAddToSpec)
                {
                    specification.Tasks.Add(task);
                }
            }

            ValidationResult jobValidation = new JobManagementValidator(specification, _unitOfWork).Validate();
            if (!jobValidation.IsValid)
            {
                _logger.ErrorFormat("Validation error: {0}", jobValidation.Message);
                ExceptionHandler.ThrowProperExternalException(new InputValidationException("Submitted job specification is not valid: \r\n" + jobValidation.Message));
            }

            lock (_lockCreateJobObj)
            {
                try
                {
                    SubmittedJobInfo jobInfo;
                    using (var transactionScope = new TransactionScope(
                            TransactionScopeOption.Required,
                            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                    {
                        _unitOfWork.JobSpecificationRepository.Insert(specification);
                        _unitOfWork.Save();//needs to be saved before SubmittedJobInfo ! -> POSSIBLE REFACTORING
                        jobInfo = CreateSubmittedJobInfo(specification);
                        _unitOfWork.SubmittedJobInfoRepository.Insert(jobInfo);
                        _unitOfWork.Save();
                        transactionScope.Complete();
                    }
                    //Create job directory
                    SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType).CreateScheduler(specification.Cluster).CreateJobDirectory(jobInfo);
                    return jobInfo;
                }
                catch (Exception e)
                {
                    _unitOfWork.Dispose();
                    _logger.Error(e);
                    throw new Exception("Transaction failed when job was creating!");
                }
            }
        }

        public virtual SubmittedJobInfo SubmitJob(long createdJobInfoId, AdaptorUser loggedUser)
        {
            _logger.Info("User " + loggedUser.GetLogIdentification() + " is submitting the job with info Id " + createdJobInfoId);
            SubmittedJobInfo jobInfo = GetSubmittedJobInfoById(createdJobInfoId, loggedUser);
            if (jobInfo.State == JobState.Configuring || jobInfo.State == JobState.WaitingForServiceAccount)
            {
                if (BusinessLogicConfiguration.ClusterAccountRotation)
                {
                    //Check if user is already running job - if yes set state to WaitingForUser - else run the job
                    lock (_lockSubmitJobObj)
                    {
                        bool isJobUserAvailable = true;
                        IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork);
                        isJobUserAvailable = clusterLogic.IsUserAvailableToRun(jobInfo.Specification.ClusterUser);

                        if (!isJobUserAvailable)
                        {
                            jobInfo.State = JobState.WaitingForServiceAccount;
                            _unitOfWork.SubmittedJobInfoRepository.Update(jobInfo);
                            _unitOfWork.Save();
                            return jobInfo;
                        }
                    }
                }
                jobInfo.SubmitTime = DateTime.UtcNow;
                var submittedTasks = SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
                                                      .CreateScheduler(jobInfo.Specification.Cluster)
                                                      .SubmitJob(jobInfo.Specification, jobInfo.Specification.ClusterUser);


                jobInfo = CombineSubmittedJobInfoFromCluster(jobInfo, submittedTasks);
                _unitOfWork.SubmittedJobInfoRepository.Update(jobInfo);
                _unitOfWork.Save();
                return jobInfo;
            }
            else
            {
                throw new InputValidationException("Submitting Job is provided only for job in state Configuring.");
            }
        }

        public virtual SubmittedJobInfo CancelJob(long submittedJobInfoId, AdaptorUser loggedUser)
        {
            _logger.Info($"User {loggedUser.GetLogIdentification()} is canceling the job with info Id {submittedJobInfoId}");
            SubmittedJobInfo jobInfo = GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            if (jobInfo.State is not JobState.Configuring and (< JobState.Finished or JobState.WaitingForServiceAccount))
            {
                var submittedTask = jobInfo.Tasks.Where(w => !w.Specification.DependsOn.Any())
                                                  .ToList();

                var scheduler = SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType).CreateScheduler(jobInfo.Specification.Cluster);
                scheduler.CancelJob(submittedTask, "Job cancelled manually by the client.", jobInfo.Specification.ClusterUser);

                var actualUnfinishedSchedulerTasksInfo = scheduler.GetActualTasksInfo(submittedTask, jobInfo.Specification.Cluster.ServiceAccountCredentials)
                                                                    .ToList();

                foreach (var task in jobInfo.Tasks)
                {
                    foreach (var actualUnfinishedSchedulerTaskInfo in actualUnfinishedSchedulerTasksInfo)
                    {
                        CombineSubmittedTaskInfoFromCluster(task, actualUnfinishedSchedulerTaskInfo);
                    }
                }

                UpdateJobStateByTasks(jobInfo);
                _unitOfWork.SubmittedJobInfoRepository.Update(jobInfo);
                _unitOfWork.Save();
            }

            return jobInfo;
        }

        public virtual void DeleteJob(long submittedJobInfoId, AdaptorUser loggedUser)
        {
            _logger.Info("User " + loggedUser.GetLogIdentification() + " is deleting the job with info Id " + submittedJobInfoId);
            SubmittedJobInfo jobInfo = GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            if (jobInfo.State is JobState.Configuring or >= JobState.Finished and not JobState.WaitingForServiceAccount)
            {
                SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType).CreateScheduler(jobInfo.Specification.Cluster).DeleteJobDirectory(jobInfo);
            }
            else
            {
                _logger.Error($"Cannot delete job with Id {submittedJobInfoId}, this job is in state {jobInfo.State}.");
                throw new InvalidRequestException($"Cannot delete job with Id {submittedJobInfoId}, this job is in state {jobInfo.State}.");
            }
        }

        public virtual SubmittedJobInfo GetSubmittedJobInfoById(long submittedJobInfoId, AdaptorUser loggedUser)
        {
            SubmittedJobInfo jobInfo = _unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId);
            if (jobInfo is null)
            {
                var message = $"Requested job info with Id: \"{submittedJobInfoId}\" does not exist in the system.";
                _logger.Error(message);
                throw new RequestedObjectDoesNotExistException(message);
            }
            if (!LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork).AuthorizeUserForJobInfo(loggedUser, jobInfo))
            {
                _logger.Error($"Logged user: \"{loggedUser.GetLogIdentification()}\" is not authorized to work with job info with id: \"{submittedJobInfoId}\". This job was submitted by user: \"{jobInfo.Submitter.GetLogIdentification()}\" for group \"{jobInfo.Specification.SubmitterGroup.Name}\"");
                throw new AdaptorUserNotAuthorizedForJobException($"Logged user: \"{loggedUser.GetLogIdentification()}\" is not authorized to work with job info with id: \"{submittedJobInfoId}\"");
            }
            return jobInfo;
        }


        public virtual SubmittedTaskInfo GetSubmittedTaskInfoById(long submittedTaskInfoId, AdaptorUser loggedUser)
        {
            SubmittedTaskInfo taskInfo = _unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskInfoId);
            if (taskInfo == null)
            {
                var message = $"Requested task info with Id: \"{submittedTaskInfoId}\" does not exist in the system.";
                _logger.Error(message);
                throw new RequestedObjectDoesNotExistException(message);
            }
            if (!LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork).AuthorizeUserForTaskInfo(loggedUser, taskInfo))
            {
                _logger.Error($"Logged user: \"{loggedUser.GetLogIdentification()}\" is not authorized to work with task info with id: \"{submittedTaskInfoId}\". This task was submitted by user: \"{taskInfo.Specification.JobSpecification.Submitter.GetLogIdentification()}\" for group \"{taskInfo.Specification.JobSpecification.SubmitterGroup.Name}\"");
                throw new AdaptorUserNotAuthorizedForJobException($"Logged user: \"{loggedUser.GetLogIdentification()}\" is not authorized to work with task info with id: \"{submittedTaskInfoId}\"");
            }
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
        /// Updates jobs in db with received info from HPC schedulers
        /// </summary>
        public void UpdateCurrentStateOfUnfinishedJobs()
        {
            var jobsGroup = _unitOfWork.SubmittedJobInfoRepository.GetAllUnfinished()
                                                                   .GroupBy(g => g.Specification.Cluster)
                                                                   .ToList();

            foreach (var jobGroup in jobsGroup)
            {
                Cluster cluster = jobGroup.Key;
                var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster);

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
                        scheduler.CancelJob(tasksExceedWaitLimit, "Job cancelled automatically by exceeding waiting limit.", userJobGroup.Key);
                    }
                }

                if (cluster.UpdateJobStateByServiceAccount.Value)
                {
                    actualUnfinishedSchedulerTasksInfo = GetActualTasksStateInHPCScheduler(scheduler, cluster.ServiceAccountCredentials, jobGroup.SelectMany(s => s.Tasks)).ToList();
                }
                else
                {
                    userJobsGroup.ForEach(f=> actualUnfinishedSchedulerTasksInfo.AddRange(GetActualTasksStateInHPCScheduler(scheduler, f.Key, f.SelectMany(s => s.Tasks))));
                }

                bool isNeedUpdateJobState = false;
                foreach (var submittedJob in jobGroup)
                {
                    foreach (var submittedTask in submittedJob.Tasks)
                    {
                        var actualUnfinishedSchedulerTaskInfo = actualUnfinishedSchedulerTasksInfo.FirstOrDefault(w => w.ScheduledJobId == submittedTask.ScheduledJobId);
                        if (actualUnfinishedSchedulerTaskInfo is null)
                        {
                            // Failed job which is not returned from schedulers
                            submittedTask.State = TaskState.Failed;
                            isNeedUpdateJobState = true;
                        }
                        else if (submittedTask.State != actualUnfinishedSchedulerTaskInfo.State)
                        {
                            SubmittedTaskInfo submittedTaskInfo = CombineSubmittedTaskInfoFromCluster(submittedTask, actualUnfinishedSchedulerTaskInfo);
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

        public void CopyJobDataToTemp(long submittedJobInfoId, AdaptorUser loggedUser, string hash, string path)
        {
            _logger.Info(string.Format("User {0} with job Id {1} is copying job data to temp {2}", loggedUser.GetLogIdentification(), submittedJobInfoId, hash));
            SubmittedJobInfo jobInfo = GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
                    .CreateScheduler(jobInfo.Specification.Cluster)
                    .CopyJobDataToTemp(jobInfo, hash, path);
        }


        public void CopyJobDataFromTemp(long createdJobInfoId, AdaptorUser loggedUser, string hash)
        {
            _logger.Info(string.Format("User {0} with job Id {1} is copying job data from temp {2}", loggedUser.GetLogIdentification(), createdJobInfoId, hash));
            SubmittedJobInfo jobInfo = GetSubmittedJobInfoById(createdJobInfoId, loggedUser);
            SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
                    .CreateScheduler(jobInfo.Specification.Cluster)
                    .CopyJobDataFromTemp(jobInfo, hash);
        }

        public IEnumerable<string> GetAllocatedNodesIPs(long submittedTaskInfoId, AdaptorUser loggedUser)
        {
            var taskInfo = GetSubmittedTaskInfoById(submittedTaskInfoId, loggedUser);
            if (taskInfo.State == TaskState.Running)
            {
                var cluster = taskInfo.Specification.JobSpecification.Cluster;
                var stringIPs = SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster).GetAllocatedNodes(taskInfo);
                return stringIPs;
            }
            else
            {
                throw new InputValidationException("Allocated nodes IP addresses are provided only for running task.");
            }
        }

        protected static bool CheckRequestedResourcesAgainstLimitations(TaskSpecification specification, ResourceUsage currentUsage)
        {

            if (currentUsage.Limitation == null || !currentUsage.Limitation.TotalMaxCores.HasValue)
            {
                return true;
            }

            int availableCores = currentUsage.Limitation.TotalMaxCores.Value - currentUsage.CoresUsed;
            if (currentUsage.Limitation.MaxCoresPerJob.HasValue && availableCores > currentUsage.Limitation.MaxCoresPerJob.Value)
            {
                _ = currentUsage.Limitation.MaxCoresPerJob.Value;
            }
            else if (availableCores < specification.MinCores)
            {
                return false;
            }
            else if (availableCores < specification.MaxCores)
            {
                specification.MaxCores = availableCores;
            }
            return true;
        }

        protected static object CombinePropertyValues(PropertyChangeSpecification propertyChange, object templateValue, object clientValue)
        {
            switch (propertyChange.ChangeMethod)
            {
                case PropertyChangeMethod.Append:
                    if (templateValue == null || (templateValue is ICollection collection && collection.Count == 0))
                    {
                        return clientValue;
                    }
                    else if (clientValue == null || (clientValue is ICollection collection2 && collection2.Count == 0))
                    {
                        return templateValue;
                    }
                    else if (clientValue is bool boolean)
                    {
                        return ((bool)templateValue) || boolean;
                    }
                    else if (clientValue is string text)
                    {
                        return ((string)templateValue) + text;
                    }
                    else if (clientValue is int integer)
                    {
                        return ((int)templateValue) + integer;
                    }
                    else if (clientValue is float single)
                    {
                        return ((float)templateValue) + single;
                    }
                    else if (clientValue is double number)
                    {
                        return ((double)templateValue) + number;
                    }
                    else if (clientValue is ICollection arrayClientValue)
                    {
                        ArrayList arrayTemplateValue = new();
                        foreach (var item in (IEnumerable)templateValue)
                        {
                            arrayTemplateValue.Add((item as ICloneable).Clone());
                        }
                        int arraySize = arrayTemplateValue.Count + arrayClientValue.Count;
                        Array returnArray = Array.CreateInstance(arrayClientValue.GetType().GetGenericArguments().Single(), arraySize);
                        arrayTemplateValue.CopyTo(returnArray, 0);
                        arrayClientValue.CopyTo(returnArray, arrayTemplateValue.Count);
                        return returnArray;
                    }
                    var msg = $"Property with name \"{propertyChange.PropertyName}\" with values \"{templateValue}\"," +
                              $"\"{clientValue}\" could not be appended because its type cannot be appended.";

                    _logger.Error(msg);
                    throw new UnableToAppendToJobTemplatePropertyException(msg);

                case PropertyChangeMethod.Rewrite:
                    return clientValue;

                default:
                    _logger.Error($"Method \"{propertyChange.ChangeMethod}\" for changing the properties values is not supported.");
                    throw new ArgumentException($"Method \"{propertyChange.ChangeMethod}\" for changing the properties values is not supported.");
            }
        }

        protected static void CombineSpecificationWithJobTemplate(JobSpecification specification, JobTemplate jobTemplate)
        {
            if (jobTemplate != null && jobTemplate.PropertyChangeSpecification != null)
            {
                foreach (PropertyChangeSpecification propertyChange in jobTemplate.PropertyChangeSpecification)
                {
                    PropertyInfo property = specification.GetType().GetProperty(propertyChange.PropertyName);
                    object clientPropertyValue = property.GetValue(specification, null);
                    if (clientPropertyValue != null)
                    {
                        if (clientPropertyValue is ICollection collection)
                        {
                            if (collection.Count == 0)
                            {
                                continue;
                            }
                        }
                        object newPropertyValue = CombinePropertyValues(propertyChange, property.GetValue(jobTemplate, null), clientPropertyValue);
                        property.SetValue(specification, newPropertyValue, null);
                    }
                }
            }
        }

        protected void CompleteJobSpecification(JobSpecification specification, AdaptorUser loggedUser, IClusterInformationLogic clusterLogic, IUserAndLimitationManagementLogic userLogic)
        {
            Cluster cluster = clusterLogic.GetClusterById(specification.ClusterId);
            specification.Cluster = cluster;

            specification.FileTransferMethod = LogicFactory.GetLogicFactory().CreateFileTransferLogic(_unitOfWork)
                                                                                .GetFileTransferMethodsByClusterId(cluster.Id)
                                                                                    .FirstOrDefault(f => f.Id == specification.FileTransferMethodId.Value);

            specification.ClusterUser = clusterLogic.GetNextAvailableUserCredentials(cluster.Id);
            specification.Submitter = loggedUser;
            specification.SubmitterGroup ??= userLogic.GetDefaultSubmitterGroup(loggedUser);

            foreach (TaskSpecification task in specification.Tasks)
            {
                CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(task.CommandTemplateId);
                if (commandTemplate != null && commandTemplate.IsGeneric)
                {
                    //dynamically get parameters and their values and parse user-defined parameters to new parameter [name at db]
                    //if you want to, refactoring is possible
                    var definedGenericCommandParameters = commandTemplate.TemplateParameters
                        .Select(x => x.Identifier);
                    var userDefinedCommandParameters = task.CommandParameterValues
                        .Where(x => !definedGenericCommandParameters.Contains(x.CommandParameterIdentifier));
                    var userScriptParameter = task.CommandParameterValues
                        .Where(x => definedGenericCommandParameters
                        .Contains(x.CommandParameterIdentifier))
                        .FirstOrDefault();
                    string userParametersParameterName = commandTemplate.TemplateParameters
                        .Where(x => x.Identifier != userScriptParameter.CommandParameterIdentifier)
                        .FirstOrDefault().Identifier;
                    string parsedUserParameter = AddGenericCommandUserDefinedCommands(userDefinedCommandParameters.ToList());

                    task.CommandParameterValues.Add(new CommandTemplateParameterValue()
                    {
                        CommandParameterIdentifier = userParametersParameterName,
                        Value = parsedUserParameter//validate if value does not contain some prohibited parameters
                    });
                    task.CommandParameterValues.RemoveAll(x => userDefinedCommandParameters.Contains(x));
                }

                CompleteTaskSpecification(task, clusterLogic);
                task.EnvironmentVariables = CombineJobAndTaskEnvironmentVariables(specification.EnvironmentVariables, task.EnvironmentVariables)
                                                .ToList();
            }
        }
        private string AddGenericCommandUserDefinedCommands(List<CommandTemplateParameterValue> templateParameters)
        {
            if (templateParameters.Count == 0)
            {
                return string.Empty;
            }

            var commandParametersSb = new StringBuilder(" \"");

            for (int i = 0; i < templateParameters.Count; i++)
            {
                var parameter = templateParameters[i];
                if (parameter.Value.Contains("\""))//todo move to validator?
                {
                    throw new ApplicationException($"Parameter '{parameter.CommandParameterIdentifier}': '{parameter.Value}' contains illegal characters.");
                }
                var parameterPair = $"{parameter.CommandParameterIdentifier}=\\\"{parameter.Value}\\\"";
                commandParametersSb.Append(parameterPair);

                if (i < templateParameters.Count - 1)
                {
                    commandParametersSb.Append(' ');
                }
            }

            commandParametersSb.Append("\"");
            return commandParametersSb.ToString();
        }

        protected void CompleteTaskSpecification(TaskSpecification taskSpecification, IClusterInformationLogic clusterLogic)
        {
            taskSpecification.ClusterNodeType = clusterLogic.GetClusterNodeTypeById(taskSpecification.ClusterNodeTypeId);
            taskSpecification.CommandTemplate = _unitOfWork.CommandTemplateRepository.GetById(taskSpecification.CommandTemplateId);

            foreach (var cmdParameterValue in taskSpecification.CommandParameterValues ?? Enumerable.Empty<CommandTemplateParameterValue>())
            {
                cmdParameterValue.TemplateParameter = _unitOfWork.CommandTemplateParameterRepository.GetByCommandTemplateIdAndCommandParamId(taskSpecification.CommandTemplateId, cmdParameterValue.CommandParameterIdentifier);
            }

            //Combination parameters from template
            taskSpecification.Priority ??= taskSpecification.ClusterNodeType.TaskTemplate.Priority;
            taskSpecification.Project ??= taskSpecification.ClusterNodeType.TaskTemplate.Project;
        }

        /// <summary>
        /// Divide extra long task to smaller tasks
        /// </summary>
        /// <param name="task">Task to divide</param>
        protected void DecomposeExtraLongTask(TaskSpecification task)
        {
            if (!(task.WalltimeLimit.HasValue))
            {
                _logger.Error($"WalltimeLimit attribute in the task {task.Name} cannot be empty.");
                throw new ArgumentNullException($"WalltimeLimit attribute in the task {task.Name} cannot be empty.");
            }

            int remainingWalltime = (int)task.WalltimeLimit;
            var dividedExtraLongTasks = new List<TaskSpecification>();
            TaskDependency dependOnLast = null;
            int iteration = 0;
            //divide extra long task to n shorter tasks

            while (remainingWalltime > 0)
            {
                TaskSpecification shorterTask = new TaskSpecification(task);
                shorterTask.Name += $"_part_{++iteration}";
                shorterTask.DependsOn = new List<TaskDependency>();
                shorterTask.Depended = new List<TaskDependency>();

                //set maximal allowed or defined remaining WalltimeLimit for ExtraLong queue
                shorterTask.WalltimeLimit = (remainingWalltime >= task.ClusterNodeType.MaxWalltime)
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
                        if (dependentsOnTask.ParentTaskSpecification.WalltimeLimit < task.ClusterNodeType.MaxWalltime)//TODO CHECK THIS!
                        {
                            dependOnLast = new TaskDependency
                            {
                                TaskSpecification = shorterTask,
                                ParentTaskSpecification = dependentsOnTask.ParentTaskSpecification
                            };
                        }
                        else//task must be dependent on some previously decomposed extra long task (last task of decomposed sequence)
                        {
                            dependOnLast = new TaskDependency
                            {
                                TaskSpecification = shorterTask,
                                ParentTaskSpecification = _extraLongTaskDecomposedDependency[dependentsOnTask.ParentTaskSpecification]
                            };
                        }
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

        protected static IEnumerable<EnvironmentVariable> CombineJobAndTaskEnvironmentVariables(IEnumerable<EnvironmentVariable> jobVariables, IEnumerable<EnvironmentVariable> taskVariables)
        {
            Dictionary<string, EnvironmentVariable> globalVariables = new();
            foreach (EnvironmentVariable jobVariable in jobVariables ?? Enumerable.Empty<EnvironmentVariable>())
            {
                globalVariables.TryAdd(jobVariable.Name, jobVariable);
            }

            foreach (EnvironmentVariable taskVariable in taskVariables ?? Enumerable.Empty<EnvironmentVariable>())
            {
                if (globalVariables.ContainsKey(taskVariable.Name))
                {
                    globalVariables[taskVariable.Name] = taskVariable;
                }
                else
                {
                    globalVariables.Add(taskVariable.Name, taskVariable);
                }
            }
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
                    .Select(s => new SubmittedTaskInfo()
                    {
                        Name = s.Name,
                        Specification = s,
                        State = TaskState.Configuring,
                        Priority = s.Priority ?? TaskPriority.Average,
                        NodeType = s.ClusterNodeType
                    })
                    .ToList()
            };
            return result;
        }

        protected static void UpdateJobStateByTasks(SubmittedJobInfo dbJobInfo)
        {
            dbJobInfo.StartTime = dbJobInfo.Tasks.FirstOrDefault()?.StartTime;
            dbJobInfo.EndTime = dbJobInfo.Tasks.Where(t => t.EndTime.HasValue).LastOrDefault()?.EndTime;
            dbJobInfo.TotalAllocatedTime = dbJobInfo.Tasks.Sum(s => s.AllocatedTime ?? 0);

            JobState continuousJobState = JobState.Finished;
            TaskState minTaskState = TaskState.Canceled;
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

        protected static SubmittedJobInfo CombineSubmittedJobInfoFromCluster(SubmittedJobInfo dbJobInfo, IEnumerable<SubmittedTaskInfo> submittedTasksInfo)
        {
            dbJobInfo.Tasks.ForEach(s => CombineSubmittedTaskInfoFromCluster(s, submittedTasksInfo.First(f => f.Name == s.Id.ToString())));

            UpdateJobStateByTasks(dbJobInfo);
            return dbJobInfo;
        }

        private static IEnumerable<SubmittedTaskInfo> GetActualTasksStateInHPCScheduler(IRexScheduler scheduler, ClusterAuthenticationCredentials credential, IEnumerable<SubmittedTaskInfo> jobTasks)
        {
            var unfinishedTasks = jobTasks.Where(w => w.State is > TaskState.Configuring and (<= TaskState.Running or TaskState.Canceled))
                                           .ToList();

            return scheduler.GetActualTasksInfo(unfinishedTasks, credential);
        }

        private static bool IsWaitingLimitExceeded(SubmittedJobInfo job)
        {
            if (job.Specification.WaitingLimit.HasValue && job.Specification.WaitingLimit > 0
                && (job.State < JobState.Running || job.State == JobState.WaitingForServiceAccount))
            {
                var waitingLimit = job.Specification.WaitingLimit.Value;
                return DateTime.UtcNow.Subtract(job.SubmitTime.Value).TotalSeconds > waitingLimit;
            }

            return false;
        }

        protected static SubmittedTaskInfo CombineSubmittedTaskInfoFromCluster(SubmittedTaskInfo dbTaskInfo, SubmittedTaskInfo clusterTaskInfo)
        {
            if (clusterTaskInfo is null)
            {
                dbTaskInfo.State = TaskState.Failed;
                return dbTaskInfo;
            }

            dbTaskInfo.TaskAllocationNodes = dbTaskInfo.TaskAllocationNodes?.Count > 0
                ? dbTaskInfo.TaskAllocationNodes.Union(clusterTaskInfo.TaskAllocationNodes, new SubmittedTaskAllocationNodeInfoComparer()).ToList()
                : dbTaskInfo.TaskAllocationNodes = clusterTaskInfo.TaskAllocationNodes;

            dbTaskInfo.ScheduledJobId = clusterTaskInfo.ScheduledJobId;
            dbTaskInfo.StartTime = clusterTaskInfo.StartTime;
            dbTaskInfo.EndTime = clusterTaskInfo.EndTime;
            dbTaskInfo.AllocatedTime = clusterTaskInfo.AllocatedTime;
            dbTaskInfo.AllocatedCores = clusterTaskInfo.AllocatedCores;
            dbTaskInfo.State = clusterTaskInfo.State;
            dbTaskInfo.AllParameters = clusterTaskInfo.AllParameters;
            dbTaskInfo.ErrorMessage = clusterTaskInfo.ErrorMessage;
            return dbTaskInfo;
        }
    }
}