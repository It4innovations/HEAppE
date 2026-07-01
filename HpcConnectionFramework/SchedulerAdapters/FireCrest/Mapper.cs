using System.Collections.Generic;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Enums;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest;

/// <summary>
///     Mapper class
/// </summary>
internal static class Mapper
{
    /// <summary>
    ///     Mapping task state from FirecRest state
    /// </summary>
    /// <param name="taskState">Task state</param>
    /// <returns></returns>
    internal static TaskState Map(this FirecRestTaskState taskState)
    {
        return taskState switch
        {
            FirecRestTaskState.Requeued
                or FirecRestTaskState.Pending
                or FirecRestTaskState.RequeueHold
                or FirecRestTaskState.RequeueFed
                or FirecRestTaskState.Configuring
                or FirecRestTaskState.ResvDelHold => TaskState.Queued,


            FirecRestTaskState.StageOut
                or FirecRestTaskState.Signaling => TaskState.Configuring,

            FirecRestTaskState.Completed
                or FirecRestTaskState.SpecialExit => TaskState.Finished,

            FirecRestTaskState.Stopped
                or FirecRestTaskState.Canceled
                or FirecRestTaskState.Suspended
                or FirecRestTaskState.Resizing => TaskState.Canceled,

            FirecRestTaskState.Running
                or FirecRestTaskState.Completing => TaskState.Running,

            FirecRestTaskState.Failed
                or FirecRestTaskState.BootFailed
                or FirecRestTaskState.NodeFail
                or FirecRestTaskState.Deadline
                or FirecRestTaskState.Timeout
                or FirecRestTaskState.OutOfMemory
                or FirecRestTaskState.Preempted
                or FirecRestTaskState.Revoked => TaskState.Failed,

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

                var paddingLength = parts[0].Length; 

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