using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.DTO;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic;

#region Data Transfer Objects (DTOs) for FireCrest JSON

internal class FireCrestJob
{
    [JsonPropertyName("jobId")] public long JobId { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("status")] public JobStatus Status { get; set; }

    [JsonPropertyName("time")] public TimeInfo Time { get; set; }

    [JsonPropertyName("nodes")] public string Nodes { get; set; }
}

internal class JobStatus
{
    [JsonPropertyName("state")] public string State { get; set; }
}

internal class TimeInfo
{
    [JsonPropertyName("start")] public long Start { get; set; }

    [JsonPropertyName("end")] public long End { get; set; }

    [JsonPropertyName("elapsed")] public int Elapsed { get; set; }
}

#endregion

public class FireCrestDataConvertor : SchedulerDataConvertor
{
    public FireCrestDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory)
    {
    }

    public override object ConvertJobSpecificationToJob(JobSpecification jobSpecification,
        object schedulerAllocationCmd)
    {
        var task = (TaskSpecification)schedulerAllocationCmd;
        var scriptBuilder = new StringBuilder();
        string baseDirectoryPath = "/home/fireuser/Identifier/HEAppE/Executions";
        string account = jobSpecification.ClusterUser?.Username ?? "default";
        string workingDirectory = $"{baseDirectoryPath}/{account}/{jobSpecification.Id}/{task.Id}".Replace("\\", "/");

        scriptBuilder.AppendLine("#!/bin/bash");
        scriptBuilder.AppendLine($"#SBATCH -J {jobSpecification.Name}");

        if (task.ClusterNodeType != null && !string.IsNullOrEmpty(task.ClusterNodeType.Queue))
        {
            scriptBuilder.AppendLine($"#SBATCH -p {task.ClusterNodeType.Queue}");
        }

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
            var nodeNames = task.RequiredNodes.Where(n => !string.IsNullOrEmpty(n.NodeName)).Select(n => n.NodeName).ToList();
            if (nodeNames.Any())
            {
                scriptBuilder.AppendLine($"#SBATCH --nodelist={string.Join(",", nodeNames)}");
            }
        }

        scriptBuilder.AppendLine($"#SBATCH -o {workingDirectory}/{task.StandardOutputFile}");
        scriptBuilder.AppendLine($"#SBATCH -e {workingDirectory}/{task.StandardErrorFile}");
        scriptBuilder.AppendLine($"#SBATCH -D {workingDirectory}");

        if (task.IsExclusive)
        {
            scriptBuilder.AppendLine("#SBATCH --exclusive");
        }

        scriptBuilder.AppendLine(task.IsRerunnable ? "#SBATCH --requeue" : "#SBATCH --no-requeue");

        if (!string.IsNullOrEmpty(task.StandardInputFile))
        {
            scriptBuilder.AppendLine($"#SBATCH -i {workingDirectory}/{task.StandardInputFile}");
        }

        if (task.EnvironmentVariables != null && task.EnvironmentVariables.Any())
        {
            var envVars = string.Join(",", task.EnvironmentVariables.Select(e => $"{e.Name}={e.Value}"));
            scriptBuilder.AppendLine($"#SBATCH --export={envVars}");
        }

        if (task.TaskParalizationSpecifications != null && task.TaskParalizationSpecifications.Any())
        {
            var parSpec = task.TaskParalizationSpecifications.First();
            if (parSpec.MPIProcesses.HasValue)
            {
                scriptBuilder.AppendLine($"#SBATCH --ntasks-per-node={parSpec.MPIProcesses.Value}");
            }
            if (parSpec.OpenMPThreads.HasValue)
            {
                scriptBuilder.AppendLine($"#SBATCH --cpus-per-task={parSpec.OpenMPThreads.Value}");
            }
        }

        if (task.ClusterNodeType?.ClusterNodeTypeAggregation != null && 
            (task.ClusterNodeType.ClusterNodeTypeAggregation.AllocationType.Contains("ACN") || 
             task.ClusterNodeType.ClusterNodeTypeAggregation.AllocationType.Contains("GPU")))
        {
            if (task.MaxCores.HasValue)
            {
                scriptBuilder.AppendLine($"#SBATCH --gpus={task.MaxCores.Value}");
            }
        }

        if (!string.IsNullOrEmpty(task.PlacementPolicy))
        {
            scriptBuilder.AppendLine($"#SBATCH --constraint={task.PlacementPolicy}");
        }

        if (task.CommandTemplate != null && !string.IsNullOrEmpty(task.CommandTemplate.ExtendedAllocationCommand))
        {
            scriptBuilder.AppendLine($"#SBATCH {task.CommandTemplate.ExtendedAllocationCommand.Trim()}");
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
        return new ClusterNodeUsage();
    }

    public override IEnumerable<string> GetJobIds(string responseMessage)
    {
        if (string.IsNullOrWhiteSpace(responseMessage))
        {
            throw new FireCrestException("UnableToParseResponse: Response from server was null or empty.")
                { CommandError = "The response from the server was empty." };
        }

        var match = Regex.Match(responseMessage, "\"job(?:I|i)d\"\\s*:\\s*(\\d+)");
        if (match.Success && match.Groups.Count > 1)
        {
            return new List<string> { match.Groups[1].Value };
        }

        throw new FireCrestException("UnableToParseResponse: Could not find 'jobid' in the JSON response.")
            { CommandError = $"The response was: {responseMessage}" };
    }

    public override SubmittedTaskInfo ConvertTaskToTaskInfo(ISchedulerJobInfo jobInfo)
    {
        throw new NotImplementedException();
    }


    public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(Cluster cluster, object responseMessage)
    {
        var response = (string)responseMessage;
        if (string.IsNullOrWhiteSpace(response))
        {
            return Enumerable.Empty<SubmittedTaskInfo>();
        }

        var submittedTasks = new List<SubmittedTaskInfo>();
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);

        if (jsonResponse.TryGetProperty("jobs", out var jobsArray) && jobsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var jobElement in jobsArray.EnumerateArray())
            {
                var taskInfo = ProcessJobElement(cluster, jobElement);
                if (taskInfo != null)
                {
                    submittedTasks.Add(taskInfo);
                }
            }
        }
        else if (jsonResponse.ValueKind == JsonValueKind.Object)
        {
            var taskInfo = ProcessJobElement(cluster, jsonResponse);
            if (taskInfo != null)
            {
                submittedTasks.Add(taskInfo);
            }
        }

        return submittedTasks;
    }

    private SubmittedTaskInfo ProcessJobElement(Cluster cluster, JsonElement jobElement)
    {
        try
        {
            var firecrestJob = JsonSerializer.Deserialize<FireCrestJob>(jobElement.GetRawText());
            if (firecrestJob == null) return null;

            var nameParts = firecrestJob.Name.Split('-');
            string taskId = nameParts.LastOrDefault() ?? firecrestJob.Name;

            var taskInfo = new SubmittedTaskInfo
            {
                ScheduledJobId = firecrestJob.JobId.ToString(),
                Name = taskId,
                State = ConvertState(firecrestJob.Status?.State),
                StartTime = ConvertFromUnixTimestamp(firecrestJob.Time?.Start),
                EndTime = ConvertFromUnixTimestamp(firecrestJob.Time?.End),
                AllocatedTime = firecrestJob.Time?.Elapsed,
                TaskAllocationNodes = ParseNodes(firecrestJob.Nodes, taskId)
            };

            return taskInfo;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CONVERTOR] ERROR: Failed to process job element. Error: {ex.Message}");
            return null;
        }
    }

    #region Helper Methods

    private DateTime? ConvertFromUnixTimestamp(long? timestamp)
    {
        if (!timestamp.HasValue || timestamp.Value <= 0) return null;
        return DateTime.UnixEpoch.AddSeconds(timestamp.Value);
    }

    private TaskState ConvertState(string state)
    {
        if (string.IsNullOrEmpty(state)) return TaskState.Unknown;
        return state.ToUpperInvariant() switch
        {
            "PENDING" => TaskState.Queued,
            "RUNNING" => TaskState.Running,
            "COMPLETED" => TaskState.Finished,
            "FAILED" => TaskState.Failed,
            "CANCELLED" => TaskState.Canceled,
            "TIMEOUT" => TaskState.Failed,
            "SUSPENDED" => TaskState.Paused,
            _ => TaskState.Unknown,
        };
    }

    private List<SubmittedTaskAllocationNodeInfo> ParseNodes(string nodesString, string taskId)
    {
        var nodeList = new List<SubmittedTaskAllocationNodeInfo>();
        if (string.IsNullOrEmpty(nodesString) ||
            nodesString.Equals("None assigned", StringComparison.OrdinalIgnoreCase))
        {
            return nodeList;
        }

        var nodeNames = nodesString.Split(',');
        long.TryParse(taskId, out long submittedTaskInfoId);

        foreach (var name in nodeNames)
        {
            nodeList.Add(new SubmittedTaskAllocationNodeInfo
            {
                AllocationNodeId = name.Trim(),
                SubmittedTaskInfoId = submittedTaskInfoId
            });
        }

        return nodeList;
    }

    #endregion
}