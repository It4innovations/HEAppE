using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using Microsoft.Extensions.Logging;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement.Callbacks;

internal class DefaultCallbackHandler : ISchedulerCallbackHandler
{
    private readonly JobManagementLogic _logic;
    private readonly ILogger _logger;

    internal DefaultCallbackHandler(JobManagementLogic logic, ILogger logger)
    {
        _logic = logic;
        _logger = logger;
    }

    public Task<long> ProcessSessionCallbackAsync(string scheduledJobId, string token, string? qSchedulerState)
    {
        _logger.LogWarning($"ProcessSessionCallbackAsync called on DefaultCallbackHandler for session '{scheduledJobId}'. This is not supported.");
        return Task.FromResult(0L);
    }

    public async Task<bool> AuthenticateTaskCallbackAsync(string token, SubmittedTaskInfo candidate, SubmittedJobInfo jobInfo, Cluster cluster)
    {
        string? masterKey = await _logic.GetMasterCallbackTokenAsync(cluster);
        if (!string.IsNullOrEmpty(masterKey))
        {
            string expectedToken = JobManagementLogic.ComputeHmac(masterKey, candidate.Id.ToString());
            if (expectedToken == token)
            {
                return true;
            }
        }
        return false;
    }

    public Task<CallbackProcessResult> ProcessTaskCallbackAsync(
        string? rawResponse,
        string? qSchedulerState,
        SubmittedTaskInfo dbTask,
        SubmittedJobInfo jobInfo,
        Cluster cluster)
    {
        if (string.IsNullOrEmpty(rawResponse))
        {
            throw new ArgumentException("Callback raw response is empty.");
        }

        var convertor = SchedulerFactory.GetInstance(cluster.SchedulerType).GetDataConvertor(_logger);
        var parsedTasks = convertor.ReadParametersFromResponse(cluster, rawResponse);
        var parsedTaskInfo = parsedTasks?.FirstOrDefault();

        if (parsedTaskInfo == null)
        {
            throw new Exception("Failed to parse callback payload using the cluster data convertor.");
        }

        var result = new CallbackProcessResult
        {
            Handled = false,
            TargetState = parsedTaskInfo.State,
            ErrorMessage = parsedTaskInfo.ErrorMessage,
            Reason = parsedTaskInfo.Reason,
            AllParameters = parsedTaskInfo.AllParameters,
            AllocatedTime = parsedTaskInfo.AllocatedTime,
            StartTime = parsedTaskInfo.StartTime,
            EndTime = parsedTaskInfo.EndTime
        };

        return Task.FromResult(result);
    }

    public Task PostProcessCallbackAsync(SubmittedTaskInfo dbTask, SubmittedJobInfo jobInfo, Cluster cluster)
    {
        // No post-processing needed for default callbacks
        return Task.CompletedTask;
    }
}
