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
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.ConversionAdapter;
using Microsoft.Extensions.Logging;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic;

#region Data Transfer Objects (DTOs) for FirecRest JSON

internal class StringToLongConverter: JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (!string.IsNullOrEmpty(str) && int.TryParse(str, out int val))
                return val;
            return default;
        }
        return reader.GetInt32();
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value != null)
            writer.WriteNumberValue((long)value);
        else
            writer.WriteNullValue();
    }
}

internal class FirecRestJob
{
    [JsonConverter(typeof(StringToLongConverter))]
    public long? JobId { get; set; }

    public string Name { get; set; }

    public JobStatus Status { get; set; }

    public string State { get; set; }

    public TimeInfo Time { get; set; }

    public string Nodes { get; set; }

    public static readonly JsonSerializerOptions DeserializationOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };


    internal class JobStatus
    {
        public string State { get; set; }
    }

    internal class TimeInfo
    {
        public long? Start { get; set; }

        public long? End { get; set; }

        public int? Elapsed { get; set; }
    }

}

#endregion

public class FirecRestDataConvertor : SchedulerDataConvertor
{

    public FirecRestDataConvertor(ConversionAdapterFactory conversionAdapterFactory, ILogger logger) : base(conversionAdapterFactory, logger)
    {
    }

    public override object ConvertJobSpecificationToJob(JobSpecification jobSpecification, object schedulerAllocationCmd)
    {
        var result = new List<(TaskSpecification, string)>();
        foreach (var taskSpec in jobSpecification.Tasks)
        {
            var script = ((string)ConvertTaskSpecificationToTask(jobSpecification, taskSpec, ""));
            result.Add((taskSpec, script));
        }
        return result;
    }


    public override object ConvertTaskSpecificationToTask(JobSpecification jobSpecification, TaskSpecification taskSpecification,
        object schedulerAllocationCmd)
    {
        var scriptBuilder = new StringBuilder();

        _conversionAdapterFactory = null;
        //_conversionAdapterFactory = new PbsProConversionAdapterFactory();
        _conversionAdapterFactory = new SlurmConversionAdapterFactory();

        if (_conversionAdapterFactory != null)
        {
            var taskScript = (string)base.ConvertTaskSpecificationToTask(jobSpecification, taskSpecification, "#!/bin/bash");
            if (_conversionAdapterFactory is SlurmConversionAdapterFactory)
            {
                taskScript = Regex.Replace(taskScript, @"^#SBATCH\s+--wrap\s+'([^']*)'\s*$", "$1", RegexOptions.Multiline); // unwrap commands
            }
            else if (_conversionAdapterFactory is PbsProConversionAdapterFactory)
            {

            }
            return taskScript;
        }

        return null;
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
            throw new FirecRestException("UnableToParseResponse: Response from server was null or empty.")
                { CommandError = "The response from the server was empty." };
        }

        using (JsonDocument document = JsonDocument.Parse(responseMessage.ToLower()))
        {
            if (document.RootElement.TryGetProperty("jobid", out var jobid))
            {
                return new List<string> { jobid.ToString() };
            }
        }

        var match = Regex.Match(responseMessage, "\"job(?:I|i)d\"\\s*:\\s*(\\d+)");
        if (match.Success && match.Groups.Count > 1)
        {
            return new List<string> { match.Groups[1].Value };
        }

        throw new FirecRestException("UnableToParseResponse: Could not find 'jobid' in the JSON response.")
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
            var jobElementJSON = jobElement.GetRawText();
            var firecrestJob = JsonSerializer.Deserialize<FirecRestJob>(jobElementJSON, FirecRestJob.DeserializationOptions);
            if (firecrestJob == null)
                return null;

            var nameParts = firecrestJob.Name.Split('-');
            string taskId = nameParts.LastOrDefault() ?? firecrestJob.Name;

            // support various versions of firecrest
            TaskState state = TaskState.Unknown;
            if (firecrestJob.Status != null)
                state = ConvertState(firecrestJob.Status?.State);
            else if (firecrestJob.State != null)
                state = ConvertState(firecrestJob.State);
            else
                throw new Exception("Cannot parse firecrest job state!");

            var taskInfo = new SubmittedTaskInfo
            {
                ScheduledJobId = firecrestJob.JobId.ToString(),
                Name = taskId,
                State = state,
                StartTime = ConvertFromUnixTimestamp(firecrestJob.Time?.Start),
                EndTime = ConvertFromUnixTimestamp(firecrestJob.Time?.End),
                AllocatedTime = firecrestJob.Time?.Elapsed,
                TaskAllocationNodes = ParseNodes(firecrestJob.Nodes, taskId)
            };

            return taskInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[CONVERTOR] ERROR: Failed to process job element. Error: {ex.Message}");
            throw;
        }
    }

    #region Helper Methods

    private DateTime? ConvertFromUnixTimestamp(long? timestamp)
    {
        if (!timestamp.HasValue || timestamp.Value <= 0)
            return null;
        return DateTime.UnixEpoch.AddSeconds(timestamp.Value);
    }

    private TaskState ConvertState(string state)
    {
        if (string.IsNullOrEmpty(state)) return TaskState.Unknown;

        var upperState = state.ToUpperInvariant();

        if (upperState.Contains("PENDING")) return TaskState.Queued;
        if (upperState.Contains("RUNNING")) return TaskState.Running;
        if (upperState.Contains("COMPLETED")) return TaskState.Finished;
        if (upperState.Contains("FAILED")) return TaskState.Failed;
        if (upperState.Contains("CANCELLED")) return TaskState.Canceled;
        if (upperState.Contains("TIMEOUT")) return TaskState.Failed;
        if (upperState.Contains("SUSPENDED")) return TaskState.Paused;
        if (upperState.Contains("NODE_FAIL")) return TaskState.Failed;

        return TaskState.Unknown;
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