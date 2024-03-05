using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.FileTransfer.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.ExtModels.JobManagement.Converts
{
    public static class JobManagementConverts
    {
        #region Methods for Object Converts
        public static JobSpecification ConvertExtToInt(this JobSpecificationExt jobSpecification, long projectId)
        {
            var result = new JobSpecification
            {
                Name = jobSpecification.Name,
                ProjectId = projectId,
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
                ClusterId = jobSpecification.ClusterId ?? 0
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
                CommandTemplateId = taskSpecificationExt.CommandTemplateId ?? 0,
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

        private static ProjectForTaskExt ConvertIntToExt(this Project project, CommandTemplate commandTemplate)
        {
            ProjectForTaskExt convert = new()
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                AccountingString = project.AccountingString,
                CommandTemplate = commandTemplate.ConvertIntToExt()
            };
            return convert;
        }

        private static ClusterNodeTypeForTaskExt ConvertIntToExt(this ClusterNodeType clusterNodeType, Project project, CommandTemplate commandTemplate)
        {
            ClusterNodeTypeForTaskExt convert = new()
            {
                Id = clusterNodeType.Id,
                Name = clusterNodeType.Name,
                Description = clusterNodeType.Description,
                Project = project.ConvertIntToExt(commandTemplate),
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
                NodeType = task.NodeType?.ConvertIntToExt(task.Project, task.Specification.CommandTemplate)
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
                EndDate = project.EndDate,
                CommandTemplates = project.CommandTemplates.Select(x => x.ConvertIntToExt()).ToArray()
            };
            return convert;
        }

        public static ExtendedProjectInfoExt ConvertIntToExtendedInfoExt(this Project project)
        {
            ExtendedProjectInfoExt convert = new()
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                AccountingString = project.AccountingString,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                PrimaryInvestigatorContact = project.ProjectContacts.FirstOrDefault(x => x.IsPI)?.Contact.Email,
                Contacts = project.ProjectContacts.OrderByDescending(x => x.IsPI).Select(x => x.Contact.Email).ToArray(),
                CommandTemplates = project.CommandTemplates.Select(x => x.ConvertIntToExt()).ToArray()
            };
            return convert;
        }

        public static ProjectResourceUsageExt ConvertIntToExt(this ProjectResourceUsage project)
        {
            ProjectResourceUsageExt convert = new()
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                AccountingString = project.AccountingString,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                NodeTypes = project.NodeTypes.Select(x => x.ConvertIntToExt()).ToArray(),
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
