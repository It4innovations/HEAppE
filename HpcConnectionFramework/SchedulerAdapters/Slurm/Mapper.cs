using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm
{
    /// <summary>
    /// Mapper class
    /// </summary>
    internal static class Mapper
    {
        /// <summary>
        /// Mapping task state from Slurm state
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
                 or SlurmTaskState.ResvDelHold => TaskState.Queued,

                SlurmTaskState.Configuring
                 or SlurmTaskState.StageOut
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
        /// Get allocated nodes for job
        /// </summary>
        /// <param name="responseMessage">Server response text</param>
        /// <returns></returns>
        internal static IEnumerable<string> GetAllocatedNodes(string responseMessage)
        {
            var allocatedNodesForJob = new List<string>();
            foreach (Match match in Regex.Matches(responseMessage, @"(?<AllocationNode>[^,]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                if (match.Success && match.Groups.Count == 2)
                {
                    var allocationValue = match.Groups.GetValueOrDefault("AllocationNode").Value;
                    if (allocationValue.Contains("[") && allocationValue.Contains("]"))
                    {
                        int openBracketIndex = allocationValue.IndexOf('[');
                        int closeBracketIndex = allocationValue.IndexOf(']');
                        var subAllocationValue = allocationValue[..openBracketIndex];
                        var rangeArrayIndexes = allocationValue[(openBracketIndex + 1)..closeBracketIndex].Split("-")
                                                                                                           .Select(s => int.Parse(s))
                                                                                                           .ToArray();
                        for (int i = rangeArrayIndexes[0]; i <= rangeArrayIndexes[1]; i++)
                        {
                            allocatedNodesForJob.Add(subAllocationValue + i.ToString("D2"));
                        }
                    }
                    else
                    {
                        allocatedNodesForJob.Add(allocationValue);
                    }
                }
            }
            return allocatedNodesForJob;
        }
    }
}
