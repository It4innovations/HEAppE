using HEAppE.DomainObjects.JobManagement;
using HEAppE.Utils.Validation;
using System.Collections.Generic;
using System.Linq;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using System.Text.RegularExpressions;
using System;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement.Validators
{
    internal class JobManagementValidator : AbstractValidator
    {
        protected readonly IUnitOfWork _unitOfWork;
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="validationObj">Validation Object</param>
        internal JobManagementValidator(object validationObj, IUnitOfWork unitOfWork)
            : base(validationObj)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion
        #region Override Methods
        /// <summary>
        /// Validation
        /// </summary>
        /// <returns></returns>
        public override ValidationResult Validate()
        {
            string message = _validationObject switch
            {
                JobSpecification jobSpecification => ValidateJobSpecification(jobSpecification),
                _ => string.Empty
            };
            return new ValidationResult(string.IsNullOrEmpty(message), message);
        }
        #endregion
        #region Local Methods
        /// <summary>
        /// Validate Job specification
        /// </summary>
        /// <param name="job">Job specification</param>
        /// <returns></returns>
        private string ValidateJobSpecification(JobSpecification job)
        {
            ValidateRequestedCluster(job);


            if (job.Id != 0 && _unitOfWork.JobSpecificationRepository.GetById(job.Id) == null)
            {
                _messageBuilder.AppendLine($"Job with Id {job.Id} does not exist in the system");
            }

            if (job.Tasks.Count <= 0)
            {
                _messageBuilder.AppendLine("Job must have at least one task");
            }

            //Task Dependency
            for (int i = 0; i < job.Tasks.Count; i++)
            {
                //Task Validation
                ValidateTaskSpecification(job.Tasks[i]);

                if (job.Tasks[i].CommandTemplate == null)
                {
                    //_messageBuilder.AppendLine($"Command Template does not exist.");
                    //this is validated in jobSpec
                    break;
                }


                //Validation cluster in tasks
                if (job.Tasks[i].CommandTemplate.ClusterNodeType.Cluster.Id != job.Cluster.Id)
                {
                    _messageBuilder.AppendLine($"Task \"{job.Tasks[i].Name}\" must used same HPC Cluster as job " +
                                               $"\"{job.Name}\".");
                }

                if (job.FileTransferMethodId != job.Tasks[i].CommandTemplate.ClusterNodeType.FileTransferMethodId)
                {
                    _messageBuilder.AppendLine($"Command template \"{job.Tasks[i].CommandTemplate.Id}\" for task " +
                                               $"\"{job.Tasks[i].Name}\" has different file transfer method " +
                                               $"\"{ job.Tasks[i].CommandTemplate.ClusterNodeType.FileTransferMethodId}\" " +
                                               $"than job file transfer method \"{job.FileTransferMethodId}\".");
                }

                if (job.Tasks[i].CommandTemplate.ClusterNodeType.Id != job.Tasks[i].ClusterNodeTypeId)
                {
                    _messageBuilder.AppendLine($"Task \"{job.Tasks[i].Name}\" must used same ClusterNodeTypeId " +
                                               $"\"{job.Tasks[i].ClusterNodeTypeId}\" which is defined in CommandTemplate " +
                                               $"(ClusterNodeTypeId=\"{job.Tasks[i].CommandTemplate.ClusterNodeType.Id}\").");
                }


                if (job.Tasks[i].DependsOn != null && job.Tasks[i].DependsOn.Count > 0)
                {
                    List<TaskSpecification> prevTasks = new List<TaskSpecification>();
                    foreach (var dependTask in job.Tasks[i].DependsOn)
                    {
                        if (dependTask.TaskSpecification == dependTask.ParentTaskSpecification)
                        {
                            //Inself reference
                            _messageBuilder.AppendLine($"Depending task \"{dependTask.TaskSpecification.Name}\" for task " +
                                                       $"\"{job.Tasks[i].Name}\" references inself.");
                        }

                        var prevTask = prevTasks.FirstOrDefault(w => ReferenceEquals(w, dependTask.ParentTaskSpecification));
                        if (prevTask is null)
                        {
                            prevTasks.Add(dependTask.ParentTaskSpecification);
                        }
                        else
                        {
                            //Same dependency
                            _messageBuilder.AppendLine($"Depending task \"{dependTask.ParentTaskSpecification.Name}\" for task " +
                                                       $"\"{job.Tasks[i].Name}\" twice same reference.");
                        }

                        bool previousTask = false;
                        for (int j = (i - 1); j >= 0; j--)
                        {
                            if (dependTask.ParentTaskSpecification == job.Tasks[j])
                            {
                                previousTask = true;
                            }
                        }

                        if (!previousTask)
                        {
                            //Circular dependency
                            _messageBuilder.AppendLine(
                                $"Depending task \"{dependTask.ParentTaskSpecification.Name}\" for task \"{job.Tasks[i].Name}\" " +
                                $"can reference only on previous task.");
                        }
                    }
                }
            }
            return _messageBuilder.ToString();
        }
        private void ValidateTaskSpecification(TaskSpecification task)
        {
            if (task.Id != 0 && _unitOfWork.TaskSpecificationRepository.GetById(task.Id) == null)
            {
                _messageBuilder.AppendLine($"Task with Id {task.Id} does not exist in the system");
            }

            ValidateWallTimeLimit(task);

            if (task.CommandTemplate == null)
            {
                _messageBuilder.AppendLine($"Command Template does not exist.");
                return;
            }

            foreach (CommandTemplateParameter parameter in task.CommandTemplate.TemplateParameters)
            {
                if (string.IsNullOrEmpty(parameter.Query) &&
                    (task.CommandParameterValues == null || 
                     !task.CommandParameterValues.Any(w => w.TemplateParameter == parameter)))
                {
                    _messageBuilder.AppendLine($"Command Template parameter \"{parameter.Identifier}\" does not have a value.");
                }
            }

            if (task.ClusterNodeTypeId != task.CommandTemplate.ClusterNodeTypeId)
            {
                _messageBuilder.AppendLine($"Task {task.Name} has wrong CommandTemplate");
            }

            if(!task.CommandTemplate.IsEnabled)
            {
                _messageBuilder.AppendLine($"Task {task.Name} has specified deleted CommandTemplateId \"{task.CommandTemplate.Id}\"");
            }

            if (task.CommandTemplate.IsGeneric)
            {
                ValidateGenericCommandTemplateSetup(task);
            }
        }

        private void ValidateWallTimeLimit(TaskSpecification task)
        {
            var clusterNodeType = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork)
                .GetClusterNodeTypeById(task.ClusterNodeTypeId);
            if (clusterNodeType == null)
            {
                _messageBuilder.AppendLine($"Requested ClusterNodeType with Id {task.ClusterNodeTypeId} does not exist in the system");
                return;
            }

            if (task.WalltimeLimit.HasValue && task.WalltimeLimit.Value > clusterNodeType.MaxWalltime)
            {
                _messageBuilder.AppendLine(
                    $"Defined task {task.Name} has set higher WalltimeLimit ({task.WalltimeLimit.Value}) than the maximum on this cluster node, " +
                    $"maximal WallTimeLimit is {clusterNodeType.MaxWalltime}");
            }
        }

        private void ValidateRequestedCluster(JobSpecification job)
        {
            var clusterNodeType = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork)
                .GetClusterById(job.ClusterId);

            if (clusterNodeType == null)
            {
                _messageBuilder.AppendLine($"Requested Cluster with Id {job.ClusterId} does not exist in the system");
            }

            if (job.FileTransferMethod?.ClusterId != job.ClusterId)
            {
                _messageBuilder.AppendLine($"Job {job.Name} has wrong FileTransferMethod");
            }
        }

        private void ValidateGenericCommandTemplateSetup(TaskSpecification task)
        {
            Dictionary<string, string> genericCommandParametres = new();
            //Regex.Matches(task.CommandTemplate.CommandParameters, @"%%\{([\w\.]+)\}", RegexOptions.Compiled)
            foreach (var commandParameterValue in task.CommandParameterValues)
            {
                var key = commandParameterValue.CommandParameterIdentifier;
                string value = commandParameterValue.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    genericCommandParametres.Add(key, value);
                }

            }

            Match scriptPathParameterName = Regex.Match(task.CommandTemplate.CommandParameters, @"%%\{([\w\.]+)\}", RegexOptions.Compiled);

            if (!scriptPathParameterName.Success)
            {
                _messageBuilder.AppendLine($"CommandTemplate is wrong");
            }
            string clusterPathToUserScript = genericCommandParametres.FirstOrDefault(x => x.Key == scriptPathParameterName.Groups[1].Value).Value;
            if (string.IsNullOrWhiteSpace(clusterPathToUserScript))
            {
                _messageBuilder.AppendLine($"User script path parameter, for generic command template, does not have a value.");
            }

            var scriptDefinedParametres = GetUserDefinedScriptParametres(task.ClusterNodeType.Cluster, clusterPathToUserScript, task.JobSpecification.ProjectId);

            foreach (string parameter in scriptDefinedParametres)
            {
                if (!genericCommandParametres.Select(x => x.Value).Any(x => Regex.IsMatch(x, $"{parameter}=\\\\\".+\\\\\"")))
                {

                    _messageBuilder.AppendLine($"Task specification does not contain '{parameter}' parameter.");
                }
            }
        }

        private IEnumerable<string> GetUserDefinedScriptParametres(Cluster cluster, string userScriptPath, long projectId)
        {
            try
            {
                var serviceAccount = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(cluster.Id, projectId);
                return SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster).GetParametersFromGenericUserScript(cluster, serviceAccount, userScriptPath).ToList();
            }
            catch (Exception)
            {
                _messageBuilder.AppendLine($"Unable to read or locate script at '{userScriptPath}'.");
                return Enumerable.Empty<string>();
            }
        }
        #endregion
    }
}
