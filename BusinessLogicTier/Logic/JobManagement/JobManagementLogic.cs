using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.JobManagement.Exceptions;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement.Exceptions;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.HpcConnectionFramework;
using log4net;
using System.Text.RegularExpressions;
using System.Text;
using HEAppE.BusinessLogicTier.Logic.ClusterInformation;
using HEAppE.BusinessLogicTier.Logic.JobManagement.Validators;
using HEAppE.Utils.Validation;
using HEAppE.DomainObjects.JobManagement.Comparers;
using System.Transactions;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement
{
    internal class JobManagementLogic : IJobManagementLogic
    {
        private readonly object _lockCreateJobObj = new();
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
                    DecomposeExtraLongTask(specification, task);
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
            if (jobInfo.State == JobState.Configuring)
            {
                SubmittedJobInfo clusterJobInfo =
                SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType)
                    .CreateScheduler(jobInfo.Specification.Cluster)
                    .SubmitJob(jobInfo.Specification, jobInfo.Specification.ClusterUser);
                jobInfo = CombineSubmittedJobInfoFromCluster(jobInfo, clusterJobInfo);
                jobInfo.SubmitTime = DateTime.UtcNow;

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
            _logger.Info("User " + loggedUser.GetLogIdentification() + " is canceling the job with info Id " + submittedJobInfoId);
            SubmittedJobInfo jobInfo = GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            if (jobInfo.State >= JobState.Finished)
            {
                return jobInfo;
            }

            string[] scheduledJobIds = jobInfo.Tasks
                .Where(t => t.Specification.DependsOn.Count == 0)
                .Select(s => s.ScheduledJobId)
                .ToArray();

            IRexScheduler scheduler = SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType).CreateScheduler(jobInfo.Specification.Cluster);
            if (jobInfo.State != JobState.Configuring)
            {
                foreach (var scheduledJobId in scheduledJobIds)
                {
                    scheduler.CancelJob(scheduledJobId, jobInfo.Specification.ClusterUser);
                }
            }
            var clusterTasksInfo = scheduler.GetActualTasksInfo(scheduledJobIds, jobInfo.Specification.Cluster);

            for (int i = 0; i < clusterTasksInfo.Length; i++)
            {
                jobInfo.Tasks[i] = CombineSubmittedTaskInfoFromCluster(jobInfo.Tasks[i], clusterTasksInfo[i]);
            }

            UpdateJobStateByTasks(jobInfo);
            _unitOfWork.SubmittedJobInfoRepository.Update(jobInfo);
            _unitOfWork.Save();
            return jobInfo;
        }

        public virtual void DeleteJob(long submittedJobInfoId, AdaptorUser loggedUser)
        {
            _logger.Info("User " + loggedUser.GetLogIdentification() + " is deleting the job with info Id " + submittedJobInfoId);
            SubmittedJobInfo jobInfo = GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            if (jobInfo.State == JobState.Configuring || jobInfo.State >= JobState.Finished)
            {
#warning Renci SSH.NET bug - resolving paths when deleting symlink, use ssh delete instead
                //FileSystemFactory.GetInstance(jobInfo.NodeType.FileTransferMethod.Protocol)
                //    .CreateFileSystemManager(jobInfo.NodeType.FileTransferMethod)
                //    .DeleteSessionFromCluster(jobInfo);
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
            if (jobInfo == null)
            {
                _logger.Error("Requested job info with Id=" + submittedJobInfoId + " does not exist in the system.");
                throw new RequestedObjectDoesNotExistException("Requested job info with Id=" + submittedJobInfoId + " does not exist in the system.");
            }
            if (!LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(_unitOfWork).AuthorizeUserForJobInfo(loggedUser, jobInfo))
            {
                _logger.Error("Logged user " + loggedUser.GetLogIdentification() + " is not authorized to work with job info with ID " + submittedJobInfoId +
                          ". This job was submitted by user " + jobInfo.Submitter.GetLogIdentification() + " for group " + jobInfo.Specification.SubmitterGroup.Name);
                throw new AdaptorUserNotAuthorizedForJobException("Logged user " + loggedUser.GetLogIdentification() + " is not authorized to work with job info with ID " +
                                                                  submittedJobInfoId);
            }
            return jobInfo;
        }

        public virtual IList<SubmittedJobInfo> ListJobsForUser(AdaptorUser loggedUser)
        {
            return _unitOfWork.SubmittedJobInfoRepository.ListAllForSubmitterId(loggedUser.Id)
                                                         .ToList();
        }

        public virtual IList<SubmittedJobInfo> ListNotFinishedJobInfosForSubmitterId(long submitterId)
        {
            return _unitOfWork.SubmittedJobInfoRepository.ListNotFinishedForSubmitterId(submitterId)
                                                         .ToList();
        }

        public virtual IList<SubmittedJobInfo> ListNotFinishedJobInfos()
        {
            return _unitOfWork.SubmittedJobInfoRepository.ListAllUnfinished()
                                                         .ToList();
        }

        private List<string> GetIterationIds(string jobArrayName, string jobArrayConfig)
        {
            int[] numbers = Regex.Split(jobArrayConfig, @"\D+").Select(x => int.Parse(x)).ToArray();
            List<string> iterations = new List<string>();

            int step = numbers.Count() == 3
                                ? numbers[2]
                                : 1;

            for (int i = numbers[0]; i <= numbers[1]; i += step)
            {
                string jobArrayNameWithIndex = jobArrayName.Insert(jobArrayName.LastIndexOf(']'), i.ToString());
                iterations.Add(jobArrayNameWithIndex);
            }
            return iterations;
        }

        private void AddValuesToJobArrayInfo(SubmittedTaskInfo jobArray, List<SubmittedTaskInfo> jobArrayIndexes)
        {
            double? allocatedTime = 0;
            var sbAllParametres = new StringBuilder();
            sbAllParametres.Append(jobArray.AllParameters);
            foreach (var jobArrayIndex in jobArrayIndexes)
            {
                foreach (var taskAllocationNode in jobArrayIndex.TaskAllocationNodes)
                {
                    if (!jobArray.TaskAllocationNodes.Exists(a => a.AllocationNodeId == taskAllocationNode.AllocationNodeId))
                    {
                        jobArray.TaskAllocationNodes.Add(taskAllocationNode);
                    }
                }
                allocatedTime += jobArrayIndex.AllocatedTime;
                jobArray.CpuHyperThreading = jobArrayIndex.CpuHyperThreading;
                sbAllParametres.AppendLine("<JOB_ARRAY_ITERATION>");
                sbAllParametres.Append(jobArrayIndex.AllParameters);
            }
            jobArray.AllocatedTime = allocatedTime;
            jobArray.AllParameters = sbAllParametres.ToString();
        }

        /// <summary>
        /// Contacts cluster and retreives info about jobs with state higher than Configuring and lower than Finished.
        /// Method updates jobs in db with received info.
        /// </summary>
        /// <returns>Collection of updated job info (contains only jobs where status change occured since last update).</returns>
        public IList<SubmittedJobInfo> UpdateCurrentStateOfUnfinishedJobs()
        {
            Dictionary<long, SubmittedJobInfo> needUpdateJobs = new Dictionary<long, SubmittedJobInfo>();

            // Load jobs
            List<SubmittedJobInfo> unfinishedJobInfoDb = (List<SubmittedJobInfo>)_unitOfWork.SubmittedJobInfoRepository.ListAllUnfinished();
            var jobsGroup = (from job in unfinishedJobInfoDb
                             group job by job.Specification.Cluster
                             into jobGroup
                             orderby jobGroup.Key
                             select jobGroup).ToList();

            foreach (var jobGroup in jobsGroup)
            {
                Cluster cluster = jobGroup.Key;
                // Get updated status from cluster
                //var jobIds = (from job in jobGroup
                //              from task in job.Tasks
                //              where job.State > JobState.Configuring && task.State > TaskState.Configuring
                //              select task.ScheduledJobId).Distinct().ToArray();
#warning retype to int from string
                var unfinishedTasks = (from job in jobGroup
                                       from task in job.Tasks
                                       where job.State > JobState.Configuring && task.State > TaskState.Configuring
                                       select (task)).Distinct().Select(s => s).ToArray();

                string[] taskIds;


                taskIds = (from task in unfinishedTasks
                           select task.ScheduledJobId).Distinct().Select(s => s).ToArray();

                List<SubmittedTaskInfo> unfinishedTaskInfoCluster =
                    (SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster).GetActualTasksInfo(taskIds, cluster)).ToList();

                var taskArrays = (from task in unfinishedTasks
                                  where !(task.Specification.JobArrays is null)
                                  select (task.ScheduledJobId, task.Specification.JobArrays)).Distinct().Select(s => s).ToArray();


                foreach (var task in taskArrays)
                {
                    var iterationIndexes = GetIterationIds(task.ScheduledJobId, task.JobArrays).ToArray();

                    List<SubmittedTaskInfo> unfinishedTaskArrayInfoCluster =
                       (SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster).GetActualTasksInfo(iterationIndexes, cluster)).ToList();
                    AddValuesToJobArrayInfo(unfinishedTaskInfoCluster.Find(x => x.ScheduledJobId == task.ScheduledJobId), unfinishedTaskArrayInfoCluster);
                }

                //TODO this
                if (unfinishedJobInfoDb.Count == 0)
                {
                    return null;
                }

                // Combine the objects together
                List<long> succSubmittedTaskIds = new List<long>();
                foreach (var clusterTask in unfinishedTaskInfoCluster)
                {
                    if (clusterTask != null)
                    {
                        // Search for tasks in db by scheduler id
                        var taskDb = (from job in jobGroup
                                      from task in job.Tasks
                                      where task.ScheduledJobId == clusterTask.ScheduledJobId
                                      select new { job, task }).Distinct().First();

                        if (taskDb.task.State != clusterTask.State)
                        {
                            // Combine together with actual info from cluster
                            SubmittedTaskInfo submittedTaskInfo = CombineSubmittedTaskInfoFromCluster(taskDb.task, clusterTask);
                            // Update and save to DB
                            _unitOfWork.SubmittedTaskInfoRepository.Update(submittedTaskInfo);
                            //add to the list of successfully retrieved cluster jobs
                            succSubmittedTaskIds.Add(submittedTaskInfo.Id);

                            // Append to output collection
                            needUpdateJobs[taskDb.job.Id] = taskDb.job;
                        }
                    }
                }
                //set missing jobIds to Canceled
                if (unfinishedTasks.Count() != unfinishedTaskInfoCluster.Count)
                {
                    var submittedTaskIds = (from job in jobGroup
                                            from task in job.Tasks
                                            where job.State > JobState.Configuring && task.State > TaskState.Configuring
                                            select new { job, task }).Distinct().ToArray();

                    foreach (var submittedTaskId in submittedTaskIds)
                    {
                        if (!succSubmittedTaskIds.Contains(submittedTaskId.task.Id))
                        {
                            //change state to Canceled
                            SubmittedTaskInfo taskInfo = _unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskId.task.Id);
                            taskInfo.State = TaskState.Canceled;
                            // Update and save to DB
                            _unitOfWork.SubmittedTaskInfoRepository.Update(taskInfo);

                            // Append to output collection
                            needUpdateJobs[submittedTaskId.job.Id] = submittedTaskId.job;
                        }
                    }
                }

                //Update JobInfo
                foreach (var jobInfo in needUpdateJobs)
                {
                    UpdateJobStateByTasks(jobInfo.Value);
                    _unitOfWork.SubmittedJobInfoRepository.Update(jobInfo.Value);
                }
            }
            // Save unit of work
            _unitOfWork.Save();

            List<SubmittedJobInfo> result = new List<SubmittedJobInfo>();
            foreach (var item in needUpdateJobs)
            {
                result.Add(item.Value);
            }

            return result;
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

        public List<string> GetAllocatedNodesIPs(long submittedJobInfoId, AdaptorUser loggedUser)
        {
            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            if (jobInfo.State == JobState.Running)
            {
                List<string> stringIPs = SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType).CreateScheduler(jobInfo.Specification.Cluster).GetAllocatedNodes(jobInfo);
                return stringIPs;
            }
            else
            {
                throw new InputValidationException("Allocated nodes IP addresses are provided only for running job.");
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
        /// <param name="specification">Specification of the job</param>
        /// <param name="task">Task to divide</param>
        protected void DecomposeExtraLongTask(JobSpecification specification, TaskSpecification task)
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
                    .Select(s => CreateSubmittedTaskInfo(s))
                    .ToList()
            };
            return result;
        }

        protected static SubmittedTaskInfo CreateSubmittedTaskInfo(TaskSpecification taskSpecification)
        {
            SubmittedTaskInfo result = new()
            {
                Name = taskSpecification.Name,
                Specification = taskSpecification,
                State = TaskState.Configuring,
                Priority = taskSpecification.Priority ?? TaskPriority.Average,
                NodeType = taskSpecification.ClusterNodeType
            };
            return result;
        }

        protected static void UpdateJobStateByTasks(SubmittedJobInfo dbJobInfo)
        {
            dbJobInfo.StartTime = dbJobInfo.Tasks.FirstOrDefault()?.StartTime;
            dbJobInfo.EndTime = dbJobInfo.Tasks.LastOrDefault()?.EndTime;
            dbJobInfo.TotalAllocatedTime = dbJobInfo.Tasks.Sum(s => s.AllocatedTime);
            dbJobInfo.State = GetGlobalJobState(dbJobInfo.Tasks.Select(s => s.State));
        }

        private static JobState GetGlobalJobState(IEnumerable<TaskState> taskStates)
        {
            if (taskStates.All(t => t.Equals(TaskState.Finished)))
            {
                return JobState.Finished;
            }
            else if (taskStates.Contains(TaskState.Failed))
            {
                return JobState.Failed;
            }
            else if (taskStates.Contains(TaskState.Running))
            {
                return JobState.Running;
            }
            else if (taskStates.Contains(TaskState.Canceled))
            {
                return JobState.Canceled;
            }
            else
            {
                return (JobState)taskStates.Min();
            }
        }

        protected static SubmittedJobInfo CombineSubmittedJobInfoFromCluster(SubmittedJobInfo dbJobInfo, SubmittedJobInfo clusterJobInfo)
        {
            dbJobInfo.Tasks = (from dbTask in dbJobInfo.Tasks
                               join clusterTask in clusterJobInfo.Tasks on dbTask.Specification.Id.ToString(CultureInfo.InvariantCulture) equals clusterTask.Name
                               select CombineSubmittedTaskInfoFromCluster(dbTask, clusterTask)).ToList();
            UpdateJobStateByTasks(dbJobInfo);
            return dbJobInfo;
        }

        protected static SubmittedTaskInfo CombineSubmittedTaskInfoFromCluster(SubmittedTaskInfo dbTaskInfo, SubmittedTaskInfo clusterTaskInfo)
        {
            dbTaskInfo.AllParameters = clusterTaskInfo.AllParameters;
            dbTaskInfo.TaskAllocationNodes = dbTaskInfo.TaskAllocationNodes?.Count > 0
                ? dbTaskInfo.TaskAllocationNodes.Union(clusterTaskInfo.TaskAllocationNodes, new SubmittedTaskAllocationNodeInfoComparer()).ToList()
                : dbTaskInfo.TaskAllocationNodes = clusterTaskInfo.TaskAllocationNodes;

            dbTaskInfo.AllocatedTime = clusterTaskInfo.AllocatedTime;
            dbTaskInfo.EndTime = clusterTaskInfo.EndTime;
            dbTaskInfo.ErrorMessage = clusterTaskInfo.ErrorMessage;
            dbTaskInfo.StartTime = clusterTaskInfo.StartTime;
            dbTaskInfo.State = clusterTaskInfo.State;
            dbTaskInfo.Priority = clusterTaskInfo.Priority;
            dbTaskInfo.ScheduledJobId = clusterTaskInfo.ScheduledJobId;
            dbTaskInfo.CpuHyperThreading = clusterTaskInfo.CpuHyperThreading;
            return dbTaskInfo;
        }
    }
}