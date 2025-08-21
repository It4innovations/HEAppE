using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.DTO;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic;

public class FireCrestDataConvertor : SchedulerDataConvertor
{
    public FireCrestDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory)
    {
    }

    public override object ConvertJobSpecificationToJob(JobSpecification jobSpecification,
        object schedulerAllocationCmd)
    {
        if (jobSpecification.Tasks == null || !jobSpecification.Tasks.Any())
        {
            throw new InvalidOperationException("JobSpecification must contain at least one task.");
        }

        var task = jobSpecification.Tasks.First();
        var scriptBuilder = new StringBuilder();

        string baseDirectoryPath = "/home/fireuser/Identifier/HEAppE/Executions";
        string account = jobSpecification.ClusterUser?.Username ?? "default";
        string workingDirectory = $"{baseDirectoryPath}/{account}/{jobSpecification.Id}/{task.Id}".Replace("\\", "/");

        scriptBuilder.AppendLine("#!/bin/bash");

        scriptBuilder.AppendLine($"#SBATCH -J {jobSpecification.Name}");

        if (jobSpecification.Project != null && !string.IsNullOrEmpty(jobSpecification.Project.AccountingString))
        {
            scriptBuilder.AppendLine($"#SBATCH -A {jobSpecification.Project.AccountingString}");
        }

        if (task.WalltimeLimit.HasValue)
        {
            var walltime = TimeSpan.FromSeconds(task.WalltimeLimit.Value);
            scriptBuilder.AppendLine($"#SBATCH -t {walltime:d\\-hh\\:mm\\:ss}");
        }

        if (task.MinCores.HasValue)
        {
            scriptBuilder.AppendLine($"#SBATCH --ntasks={task.MinCores.Value}");
        }

        if (task.RequiredNodes != null && task.RequiredNodes.Any())
        {
            scriptBuilder.AppendLine($"#SBATCH --nodes={task.RequiredNodes.Count}");
        }

        scriptBuilder.AppendLine($"#SBATCH -o {workingDirectory}/{task.StandardOutputFile}");
        scriptBuilder.AppendLine($"#SBATCH -e {workingDirectory}/{task.StandardErrorFile}");
        scriptBuilder.AppendLine($"#SBATCH -D {workingDirectory}");

        if (task.IsExclusive)
        {
            scriptBuilder.AppendLine("#SBATCH --exclusive");
        }

        if (jobSpecification.NotifyOnStart == true || jobSpecification.NotifyOnFinish == true ||
            jobSpecification.NotifyOnAbort == true)
        {
            var mailType = new List<string>();
            if (jobSpecification.NotifyOnStart == true) mailType.Add("BEGIN");
            if (jobSpecification.NotifyOnFinish == true) mailType.Add("END");
            if (jobSpecification.NotifyOnAbort == true) mailType.Add("FAIL");

            if (mailType.Any())
            {
                scriptBuilder.AppendLine($"#SBATCH --mail-type={string.Join(",", mailType)}");
                if (!string.IsNullOrEmpty(jobSpecification.NotificationEmail))
                {
                    scriptBuilder.AppendLine($"#SBATCH --mail-user={jobSpecification.NotificationEmail}");
                }
            }
        }

        scriptBuilder.AppendLine();

        string commandToExecute = GetCommandFromTemplate(task);
        scriptBuilder.AppendLine(commandToExecute);

        return scriptBuilder.ToString();
    }

    private string GetCommandFromTemplate(TaskSpecification task)
    {
        if (task.CommandTemplate == null)
        {
            throw new InvalidOperationException($"Command template for Task ID {task.Id} is not loaded or is missing.");
        }

        var commandBuilder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(task.CommandTemplate.PreparationScript))
        {
            commandBuilder.AppendLine(task.CommandTemplate.PreparationScript);
        }

        commandBuilder.Append(task.CommandTemplate.ExecutableFile);

        if (!string.IsNullOrWhiteSpace(task.CommandTemplate.CommandParameters))
        {
            commandBuilder.Append($" {task.CommandTemplate.CommandParameters}");
        }

        string commandTemplateString = commandBuilder.ToString();

        if (task.CommandParameterValues == null || !task.CommandParameterValues.Any())
        {
            return commandTemplateString;
        }

        foreach (var parameter in task.CommandParameterValues)
        {
            if (parameter.TemplateParameter == null || string.IsNullOrEmpty(parameter.TemplateParameter.Identifier))
            {
                continue;
            }

            string placeholder = $"{{{parameter.TemplateParameter.Identifier}}}";
            string value = parameter.Value ?? string.Empty;
            commandTemplateString = commandTemplateString.Replace(placeholder, value);
        }

        return commandTemplateString;
    }

    public override ClusterNodeUsage ReadQueueActualInformation(ClusterNodeType nodeType, object responseMessage)
    {
        var nodesUsed = 0;
        var response = (string)responseMessage;
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);

        if (string.IsNullOrEmpty(nodeType.ClusterAllocationName))
        {
            if (jsonResponse.TryGetProperty("total_allocated_nodes", out var totalNodes))
            {
                nodesUsed = totalNodes.GetInt32();
            }
        }
        else
        {
            if (jsonResponse.TryGetProperty("partitions", out var partitions) &&
                partitions.ValueKind == JsonValueKind.Array)
            {
                foreach (var partition in partitions.EnumerateArray())
                {
                    if (partition.TryGetProperty("name", out var name) &&
                        name.GetString() == nodeType.ClusterAllocationName &&
                        partition.TryGetProperty("allocated_nodes", out var allocatedNodes))
                    {
                        nodesUsed = allocatedNodes.GetInt32();
                        break;
                    }
                }
            }
        }

        return new ClusterNodeUsage
        {
            NodeType = nodeType,
            NodesUsed = nodesUsed,
            Priority = default,
            TotalJobs = default
        };
    }

    public override IEnumerable<string> GetJobIds(string responseMessage)
    {
        if (string.IsNullOrWhiteSpace(responseMessage))
        {
            throw new FireCrestException("UnableToParseResponse: Response from server was null or empty.")
            {
                CommandError = "The response from the server was empty."
            };
        }

        var match = Regex.Match(responseMessage, "\"jobid\":\\s*(\\d+)");

        if (match.Success && match.Groups.Count > 1)
        {
            return new List<string> { match.Groups[1].Value };
        }

        match = Regex.Match(responseMessage, "\"jobId\":\\s*(\\d+)");
        if (match.Success && match.Groups.Count > 1)
        {
            return new List<string> { match.Groups[1].Value };
        }

        throw new FireCrestException("UnableToParseResponse: Could not find 'jobid' in the response.")
        {
            CommandError = $"The response was: {responseMessage}"
        };
    }

    public override SubmittedTaskInfo ConvertTaskToTaskInfo(ISchedulerJobInfo jobInfo)
    {
        var obj = (FireCrestJobInfo)jobInfo;
        return new SubmittedTaskInfo
        {
            ScheduledJobId = obj.SchedulerJobId,
            Name = obj.Name,
            StartTime = obj.StartTime,
            EndTime = obj.StartTime.HasValue && obj.TaskState >= TaskState.Finished ? obj.EndTime : null,
            AllocatedTime = Math.Round(obj.RunTime.TotalSeconds, 3),
            AllocatedCores = obj.UsedCores,
            State = obj.IsDeadLock ? TaskState.Failed : obj.TaskState,
            TaskAllocationNodes = obj.AllocatedNodes?.Select(s => new SubmittedTaskAllocationNodeInfo
                    { AllocationNodeId = s, SubmittedTaskInfoId = long.Parse(obj.Name) })
                .ToList(),
            ErrorMessage = default,
            Reason = obj.Reason,
            AllParameters = obj.IsJobArrayJob
                ? obj.AggregateSchedulerResponseParameters
                : obj.SchedulerResponseParameters,
            ParsedParameters = obj.ParsedParameters
        };
    }

    public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(Cluster cluster, object responseMessage)
    {
        var response = (string)responseMessage;
        var jobSubmitedTasksInfo = new List<SubmittedTaskInfo>();
        FireCrestJobInfo aggregateResultObj = null;

        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
        if (!jsonResponse.TryGetProperty("jobs", out var jobsArray))
        {
            throw new FireCrestException("UnableToParseResponse: Response JSON does not contain a 'jobs' array.")
            {
                CommandError = response
            };
        }

        foreach (var jobElement in jobsArray.EnumerateArray())
        {
            var jobResponseMessage = JsonSerializer.Serialize(jobElement);
            var parsedParameters = new Dictionary<string, string>();

            foreach (var property in jobElement.EnumerateObject())
            {
                var value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => string.Empty,
                    _ => JsonSerializer.Serialize(property.Value)
                };

                if (value is "(null)" or "N/A" or "Unknown")
                    value = string.Empty;

                parsedParameters.Add(property.Name, value);
            }

            var schedulerResultObj = new FireCrestJobInfo(jobResponseMessage, parsedParameters);
            FillingSchedulerJobResultObjectFromSchedulerAttribute(cluster, schedulerResultObj, parsedParameters);

            if (!schedulerResultObj.IsJobArrayJob)
            {
                jobSubmitedTasksInfo.Add(ConvertTaskToTaskInfo(schedulerResultObj));
                continue;
            }

            if (aggregateResultObj is null)
            {
                aggregateResultObj = schedulerResultObj;
                continue;
            }

            if (aggregateResultObj.ArrayJobId == schedulerResultObj.ArrayJobId)
            {
                aggregateResultObj.CombineJobs(schedulerResultObj);
                continue;
            }

            jobSubmitedTasksInfo.Add(ConvertTaskToTaskInfo(aggregateResultObj));
            aggregateResultObj = schedulerResultObj;
        }

        if (aggregateResultObj is not null)
            jobSubmitedTasksInfo.Add(ConvertTaskToTaskInfo(aggregateResultObj));

        return jobSubmitedTasksInfo.Any()
            ? jobSubmitedTasksInfo
            : new List<SubmittedTaskInfo>();
    }
}