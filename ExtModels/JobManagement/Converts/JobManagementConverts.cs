using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.FileTransfer.Converts;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.JobManagement.Converts
{
    public static class JobManagementConverts
    {
        #region Methods for Object Converts
        public static JobSpecification ConvertExtToInt(this JobSpecificationExt jobSpecification, long? projectId)
        {
            var result = new JobSpecification
            {
                Name = jobSpecification.Name,
                ProjectId = projectId.HasValue?projectId.Value:0,//todo SINGLE PROJECT HEAPPE INSTANCE
                WaitingLimit = jobSpecification.WaitingLimit ?? 0,
                WalltimeLimit = jobSpecification.WalltimeLimit,
                NotificationEmail = jobSpecification.NotificationEmail,
                PhoneNumber = jobSpecification.PhoneNumber,
                NotifyOnAbort = jobSpecification.NotifyOnAbort,
                NotifyOnFinish = jobSpecification.NotifyOnFinish,
                NotifyOnStart = jobSpecification.NotifyOnStart,
                EnvironmentVariables = jobSpecification.EnvironmentVariables?
                                                        .Select(s => s.ConvertExtToInt())
                                                        .ToList(),
                FileTransferMethodId = jobSpecification.FileTransferMethodId,
                ClusterId = jobSpecification.ClusterId??0
            };

            //Same Reference for DependOn tasks
            Dictionary<TaskSpecificationExt, TaskSpecification> tasksSpecs = new();
            foreach (var taskExt in jobSpecification.Tasks)
            {
                var convertedTaskSpec = taskExt.ConvertExtToInt(result);
                if (taskExt.DependsOn != null)
                {
                    var taskDependency = new List<TaskDependency>();
                    foreach (var dependentTask in taskExt.DependsOn)
                    {
                        if (tasksSpecs.ContainsKey(dependentTask))
                        {
                            taskDependency.Add(new TaskDependency
                            {
                                TaskSpecification = convertedTaskSpec,
                                ParentTaskSpecification = tasksSpecs[dependentTask]
                            });
                        }
                        else
                        {
                            //throw new InputValidationException($"Depending task \"{dependentTask.Name}\" for task \"{taskExt.Name}\" contains wrong task dependency.");
                        }
                    }
                    convertedTaskSpec.DependsOn = taskDependency;
                }
                tasksSpecs.Add(taskExt, convertedTaskSpec);
            }
            result.Tasks = tasksSpecs.Values.ToList();

            //Agregation walltimelimit for tasks
            result.WalltimeLimit = result.Tasks.Sum(s => s.WalltimeLimit);
            return result;
        }

        private static TaskSpecification ConvertExtToInt(this TaskSpecificationExt taskSpecificationExt, JobSpecification jobSpecification)
        {
            var result = new TaskSpecification
            {
                Name = taskSpecificationExt.Name,
                MinCores = taskSpecificationExt.MinCores,
                MaxCores = taskSpecificationExt.MaxCores,
                WalltimeLimit = taskSpecificationExt.WalltimeLimit,
                PlacementPolicy = taskSpecificationExt.PlacementPolicy,
                RequiredNodes = taskSpecificationExt.RequiredNodes?
                                                     .Select(s => new TaskSpecificationRequiredNode
                                                     {
                                                         NodeName = s
                                                     })
                                                     .ToList(),
                Priority = taskSpecificationExt.Priority.ConvertExtToInt(),
                Project = jobSpecification.Project,
                JobArrays = taskSpecificationExt.JobArrays,
                IsExclusive = taskSpecificationExt.IsExclusive ?? false,
                IsRerunnable = !string.IsNullOrEmpty(taskSpecificationExt.JobArrays) || (taskSpecificationExt.IsRerunnable ?? false),
                StandardInputFile = taskSpecificationExt.StandardInputFile,
                StandardOutputFile = taskSpecificationExt.StandardOutputFile ?? "stdout.txt",
                StandardErrorFile = taskSpecificationExt.StandardErrorFile ?? "stderr.txt",
                ClusterTaskSubdirectory = taskSpecificationExt.ClusterTaskSubdirectory,
                ProgressFile = new FileSpecification
                {
                    RelativePath = taskSpecificationExt.ProgressFile,
                    NameSpecification = FileNameSpecification.FullName,
                    SynchronizationType = FileSynchronizationType.IncrementalAppend
                },
                LogFile = new FileSpecification
                {
                    RelativePath = taskSpecificationExt.LogFile,
                    NameSpecification = FileNameSpecification.FullName,
                    SynchronizationType = FileSynchronizationType.IncrementalAppend
                },
                ClusterNodeTypeId = taskSpecificationExt.ClusterNodeTypeId.Value,
                CommandTemplateId = taskSpecificationExt.CommandTemplateId??0,
                EnvironmentVariables = taskSpecificationExt.EnvironmentVariables?
                                                            .Select(s => s.ConvertExtToInt())
                                                            .ToList(),
                CpuHyperThreading = taskSpecificationExt.CpuHyperThreading,
                JobSpecification = jobSpecification,
                TaskParalizationSpecifications = taskSpecificationExt.TaskParalizationParameters?
                                                                      .Select(s => s.ConvertExtToInt())
                                                                      .ToList(),
                CommandParameterValues = taskSpecificationExt.TemplateParameterValues?
                                                              .Select(s => s.ConvertExtToInt())
                                                              .ToList(),

            };

            result.DependsOn = taskSpecificationExt.DependsOn?
                                                    .Select(s => new TaskDependency
                                                    {
                                                        TaskSpecification = result,
                                                        ParentTaskSpecification = s.ConvertExtToInt(jobSpecification)
                                                    })
                                                    .ToList();
            return result;
        }

        private static EnvironmentVariable ConvertExtToInt(this EnvironmentVariableExt environmentVariableExt)
        {
            var convert = new EnvironmentVariable
            {
                Name = environmentVariableExt.Name,
                Value = environmentVariableExt.Value
            };
            return convert;
        }

        private static TaskParalizationSpecification ConvertExtToInt(this TaskParalizationParameterExt taskParalizationSpecification)
        {
            var convert = new TaskParalizationSpecification
            {
                MaxCores = taskParalizationSpecification.MaxCores,
                MPIProcesses = taskParalizationSpecification.MPIProcesses,
                OpenMPThreads = taskParalizationSpecification.OpenMPThreads
            };
            return convert;
        }

        private static CommandTemplateParameterValue ConvertExtToInt(this CommandTemplateParameterValueExt parameterValueExt)
        {
            CommandTemplateParameterValue convert = new()
            {
                CommandParameterIdentifier = parameterValueExt.CommandParameterIdentifier,
                Value = parameterValueExt.ParameterValue
            };
            return convert;
        }

        public static SubmittedJobInfoExt ConvertIntToExt(this SubmittedJobInfo jobInfo)
        {
            SubmittedJobInfoExt convert = new()
            {
                Id = jobInfo.Id,
                Name = jobInfo.Name,
                State = jobInfo.State.ConvertIntToExt(),
                Project = jobInfo.Project?.ConvertIntToExt(),
                CreationTime = jobInfo.CreationTime,
                SubmitTime = jobInfo.SubmitTime,
                StartTime = jobInfo.StartTime,
                EndTime = jobInfo.EndTime,
                TotalAllocatedTime = jobInfo.TotalAllocatedTime,
                Tasks = jobInfo.Tasks.Select(s => s.ConvertIntToExt())
                                       .ToArray()
            };
            return convert;
        }

        private static SubmittedTaskInfoExt ConvertIntToExt(this SubmittedTaskInfo task)
        {
            SubmittedTaskInfoExt convert = new()
            {
                Id = task.Id,
                Name = task.Name,
                State = task.State.ConvertIntToExt(),
                Priority = task.Priority.ConvertIntToExt(),
                AllocatedTime = task.AllocatedTime,
                AllocatedCoreIds = task.TaskAllocationNodes?.Select(s => s.AllocationNodeId)
                                                             .ToArray(),
                StartTime = task.StartTime,
                EndTime = task.EndTime,
                CpuHyperThreading = task.CpuHyperThreading,
                ErrorMessage = task.ErrorMessage,
                NodeType = task.NodeType?.ConvertIntToExt()
            };
            return convert;
        }

        public static ProjectExt ConvertIntToExt(this Project project)
        {
            ProjectExt convert = new()
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                AccountingString = project.AccountingString,
                StartDate = project.StartDate,
                EndDate = project.EndDate
            };
            return convert;
        }
        #endregion
        #region Methods for Enums Converts
        private static TaskPriority ConvertExtToInt(this TaskPriorityExt? priority)
        {
            if (priority.HasValue)
            {
                _ = Enum.TryParse(priority.ToString(), out TaskPriority convert);
                return convert;
            }
            else
            {
                return TaskPriority.Average;
            }
        }

        public static JobStateExt ConvertIntToExt(this JobState jobState)
        {
            _ = Enum.TryParse(jobState.ToString(), out JobStateExt convert);
            return convert;
        }

        public static TaskStateExt ConvertIntToExt(this TaskState taskState)
        {
            _ = Enum.TryParse(taskState.ToString(), out TaskStateExt convert);
            return convert;
        }

        public static TaskPriorityExt ConvertIntToExt(this TaskPriority taskPriority)
        {
            _ = Enum.TryParse(taskPriority.ToString(), out TaskPriorityExt convert);
            return convert;
        }
        #endregion
    }
}
