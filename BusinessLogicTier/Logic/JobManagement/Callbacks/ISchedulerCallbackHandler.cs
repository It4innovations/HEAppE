using System;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement.Callbacks;

internal interface ISchedulerCallbackHandler
{
    Task<long> ProcessSessionCallbackAsync(string scheduledJobId, string token, string? qSchedulerState);

    Task<bool> AuthenticateTaskCallbackAsync(string token, SubmittedTaskInfo candidate, SubmittedJobInfo jobInfo, Cluster cluster);

    Task<CallbackProcessResult> ProcessTaskCallbackAsync(
        string? rawResponse,
        string? qSchedulerState,
        SubmittedTaskInfo dbTask,
        SubmittedJobInfo jobInfo,
        Cluster cluster);

    Task PostProcessCallbackAsync(SubmittedTaskInfo dbTask, SubmittedJobInfo jobInfo, Cluster cluster);
}

public class CallbackProcessResult
{
    public bool Handled { get; set; }
    public long JobId { get; set; }
    public TaskState TargetState { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Reason { get; set; }
    public string? AllParameters { get; set; }
    public double? AllocatedTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}
