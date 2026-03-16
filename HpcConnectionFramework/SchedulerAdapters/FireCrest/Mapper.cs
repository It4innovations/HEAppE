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
    ///     Mapping task state from FireCrest state
    /// </summary>
    /// <param name="taskState">Task state</param>
    /// <returns></returns>
    internal static TaskState Map(this FireCrestTaskState taskState)
    {
        return taskState switch
        {
            FireCrestTaskState.Requeued
                or FireCrestTaskState.Pending
                or FireCrestTaskState.RequeueHold
                or FireCrestTaskState.RequeueFed
                or FireCrestTaskState.Configuring
                or FireCrestTaskState.ResvDelHold => TaskState.Queued,


            FireCrestTaskState.StageOut
                or FireCrestTaskState.Signaling => TaskState.Configuring,

            FireCrestTaskState.Completed
                or FireCrestTaskState.SpecialExit => TaskState.Finished,

            FireCrestTaskState.Stopped
                or FireCrestTaskState.Canceled
                or FireCrestTaskState.Suspended
                or FireCrestTaskState.Resizing => TaskState.Canceled,

            FireCrestTaskState.Running
                or FireCrestTaskState.Completing => TaskState.Running,

            FireCrestTaskState.Failed
                or FireCrestTaskState.BootFailed
                or FireCrestTaskState.NodeFail
                or FireCrestTaskState.Deadline
                or FireCrestTaskState.Timeout
                or FireCrestTaskState.OutOfMemory
                or FireCrestTaskState.Preempted
                or FireCrestTaskState.Revoked => TaskState.Failed,

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