using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.Management;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.Management.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;

namespace HEAppE.ExtModels.JobManagement.Converts;

public static class JobManagementConverts
{
    #region Methods for Object Converts

    public static JobSpecification ConvertExtToInt(this JobSpecificationExt jobSpecification, long projectId,
        long? subProject)
    {
        var result = new JobSpecification
        {
            Name = jobSpecification.Name,
            ProjectId = projectId,
            SubProjectId = subProject,
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
                    if (tasksSpecs.ContainsKey(dependentTask))
                        taskDependency.Add(new TaskDependency
                        {
                            TaskSpecification = convertedTaskSpec,
                            ParentTaskSpecification = tasksSpecs[dependentTask]
                        });
                convertedTaskSpec.DependsOn = taskDependency;
            }

            tasksSpecs.Add(taskExt, convertedTaskSpec);
        }

        result.Tasks = tasksSpecs.Values.ToList();

        //Agregation walltimelimit for tasks
        result.WalltimeLimit = result.Tasks.Sum(s => s.WalltimeLimit);
        return result;
    }

    private static TaskSpecification ConvertExtToInt(this TaskSpecificationExt taskSpecificationExt,
        JobSpecification jobSpecification)
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
            IsRerunnable = !string.IsNullOrEmpty(taskSpecificationExt.JobArrays) ||
                           (taskSpecificationExt.IsRerunnable ?? false),
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
            TaskParalizationSpecifications = taskSpecificationExt.TaskParallelizationParameters?
                .Select(s => s.ConvertExtToInt())
                .ToList(),
            CommandParameterValues = taskSpecificationExt.TemplateParameterValues?
                .Select(s => s.ConvertExtToInt())
                .ToList()
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

    private static TaskParalizationSpecification ConvertExtToInt(
        this TaskParalizationParameterExt taskParalizationSpecification)
    {
        var convert = new TaskParalizationSpecification
        {
            MaxCores = taskParalizationSpecification.MaxCores,
            MPIProcesses = taskParalizationSpecification.MPIProcesses,
            OpenMPThreads = taskParalizationSpecification.OpenMPThreads
        };
        return convert;
    }

    private static CommandTemplateParameterValue ConvertExtToInt(
        this CommandTemplateParameterValueExt parameterValueExt)
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
            SubProject = jobInfo.Specification.SubProject?.Identifier,
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

    private static ClusterNodeTypeForTaskExt ConvertIntToExt(this ClusterNodeType clusterNodeType, Project project,
        CommandTemplate commandTemplate)
    {
        ClusterNodeTypeForTaskExt convert = new()
        {
            Id = clusterNodeType.Id,
            Name = clusterNodeType.Name,
            Description = clusterNodeType.Description,
            Project = project.ConvertIntToExt(commandTemplate)
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
            Reason = task.Reason,
            NodeType = task.NodeType == null
                ? null
                : task.NodeType?.ConvertIntToExt(task.Project, task.Specification.CommandTemplate)
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
            UsageType = project.UsageType.ConvertIntToExt(),
            UseAccountingStringForScheduler = project.UseAccountingStringForScheduler,
            IsOneToOneMapping = project.IsOneToOneMapping,
            CommandTemplates = project.CommandTemplates.Select(x => x.ConvertIntToExt()).ToArray()
        };
        return convert;
    }

    public static UsageTypeExt ConvertIntToExt(this UsageType usageType)
    {
        switch (usageType)
        {
            case UsageType.NodeHours:
                return UsageTypeExt.NodeHours;
            case UsageType.CoreHours:
                return UsageTypeExt.CoreHours;
            default:
                return UsageTypeExt.NodeHours;
        }
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
            NodeTypes = project.NodeTypes.Select(x => x.ConvertIntToExt()).ToArray()
        };
        return convert;
    }

    public static AccountingExt ConvertIntToExt(this Accounting accounting)
    {
        AccountingExt convert = new()
        {
            Id = accounting.Id,
            Formula = accounting.Formula,
            CreatedAt = accounting.CreatedAt,
            ModifiedAt = accounting.ModifiedAt,
            ValidityFrom = accounting.ValidityFrom,
            ValidityTo = accounting.ValidityTo
        };
        return convert;
    }

    public static ProjectClusterNodeTypeAggregationExt ConvertIntToExt(
        this ProjectClusterNodeTypeAggregation aggregation)
    {
        var convert = new ProjectClusterNodeTypeAggregationExt
        {
            ProjectId = aggregation.ProjectId,
            ClusterNodeTypeAggregationId = aggregation.ClusterNodeTypeAggregationId,
            AllocationAmount = aggregation.AllocationAmount,
            CreatedAt = aggregation.CreatedAt,
            ModifiedAt = aggregation.ModifiedAt
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

        return TaskPriority.Average;
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

    public static StatusExt ConvertIntToExt(this Status status)
    {
        var convert = new StatusExt()
        {
            ProjectId = status.ProjectId,
            TimeFrom = status.TimeFrom,
            TimeTo = status.TimeTo,
            Statistics = new StatusExt.StatisticsExt_
            {
                TotalChecks = status.Statistics.TotalChecks,
                VaultCredential = new StatusExt.VaultCredentialCountsExt_()
                {
                    OkCount = status.Statistics.VaultCredential.OkCount,
                    FailCount = status.Statistics.VaultCredential.FailCount
                },
                ClusterConnection = new StatusExt.ClusterConnectionCountsExt_()
                {
                    OkCount = status.Statistics.ClusterConnection.OkCount,
                    FailCount = status.Statistics.ClusterConnection.FailCount
                },
                DryRunJob = new StatusExt.ClusterConnectionCountsExt_()
                {
                    OkCount = status.Statistics.DryRunJob.OkCount,
                    FailCount = status.Statistics.DryRunJob.FailCount
                }
            },
            Details = new List<StatusExt.DetailExt_>()
        };

        foreach (var detail in status.Details)
        {
            List<StatusExt.DetailExt_.CheckLogExt_> errors = null;

            (convert.Details as List<StatusExt.DetailExt_>).Add(new StatusExt.DetailExt_()
            {
                CheckTimestamp = detail.CheckTimestamp,
                ClusterAuthenticationCredential = new StatusExt.DetailExt_.ClusterAuthenticationCredentialExt_()
                {
                    Id = detail.ClusterAuthenticationCredential.Id,
                    Username = detail.ClusterAuthenticationCredential.Username,
                },
                VaultCredential = new StatusExt.VaultCredentialCountsExt_()
                {
                    OkCount = detail.VaultCredential.OkCount,
                    FailCount = detail.VaultCredential.FailCount
                },
                ClusterConnection = new StatusExt.ClusterConnectionCountsExt_()
                {
                    OkCount = detail.ClusterConnection.OkCount,
                    FailCount = detail.ClusterConnection.FailCount
                },
                DryRunJob = new StatusExt.DryRunJobCountsExt_()
                {
                    OkCount = detail.DryRunJob.OkCount,
                    FailCount = detail.DryRunJob.FailCount
                },
                Errors = errors
            });
        }

        return convert;
    }

    #endregion
}