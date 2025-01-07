using System.Collections.Generic;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Enums;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm;

/// <summary>
///     Mapper class
/// </summary>
internal static class Mapper
{
    /// <summary>
    ///     Mapping task state from Slurm state
    /// </summary>
    /// <param name="taskState">Task state</param>
    /// <returns></returns>
    internal static TaskState Map(this SlurmTaskState taskState)
    {
        return taskState switch
        {
            SlurmTaskState.Requeued
                or SlurmTaskState.Pending
                or SlurmTaskState.RequeueHold
                or SlurmTaskState.RequeueFed
                or SlurmTaskState.Configuring
                or SlurmTaskState.ResvDelHold => TaskState.Queued,


            SlurmTaskState.StageOut
                or SlurmTaskState.Signaling => TaskState.Configuring,

            SlurmTaskState.Completed
                or SlurmTaskState.SpecialExit => TaskState.Finished,

            SlurmTaskState.Stopped
                or SlurmTaskState.Canceled
                or SlurmTaskState.Suspended
                or SlurmTaskState.Resizing => TaskState.Canceled,

            SlurmTaskState.Running
                or SlurmTaskState.Completing => TaskState.Running,

            SlurmTaskState.Failed
                or SlurmTaskState.BootFailed
                or SlurmTaskState.NodeFail
                or SlurmTaskState.Deadline
                or SlurmTaskState.Timeout
                or SlurmTaskState.OutOfMemory
                or SlurmTaskState.Preempted
                or SlurmTaskState.Revoked => TaskState.Failed,

            _ => TaskState.Failed
        };
    }

    /// <summary>
    ///     Get allocated nodes for job
    /// </summary>
    /// <param name="responseMessage">Server response text</param>
    /// <returns></returns>
    internal static IEnumerable<string> GetAllocatedNodes(string responseMessage)
    {
        var nodes = new List<string>();
        var pattern = @"([^\[,]+)(\[[^\]]+\])?";

        foreach (Match match in Regex.Matches(responseMessage, pattern))
        {
            var nodeName = match.Groups[1].Value;
            var rangePart = match.Groups[2].Value;

            if (string.IsNullOrEmpty(rangePart))
            {
                nodes.Add(nodeName);
            }
            else
            {
                var expandedNodes = ExpandRange(nodeName, rangePart);
                nodes.AddRange(expandedNodes);
            }
        }

        return nodes;
    }

    private static List<string> ExpandRange(string nodeName, string rangePart)
    {
        var expandedNodes = new List<string>();

        var ranges = rangePart.Trim('[', ']').Split(',');

        foreach (var range in ranges)
            if (range.Contains("-"))
            {
                var parts = range.Split('-');
                var start = int.Parse(parts[0]);
                var end = int.Parse(parts[1]);

                var paddingLength = parts[0].Length; // Get the length of the first part

                for (var i = start; i <= end; i++)
                {
                    var paddedI = i.ToString($"D{paddingLength}");
                    expandedNodes.Add($"{nodeName}{paddedI}");
                }
            }
            else
            {
                expandedNodes.Add($"{nodeName}{range}");
            }

        return expandedNodes;
    }
}