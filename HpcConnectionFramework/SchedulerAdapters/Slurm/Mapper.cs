using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Enums;
using System;
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
            switch (taskState)
            {
                case SlurmTaskState.Requeued:
                case SlurmTaskState.Pending:
                case SlurmTaskState.RequeueHold:
                case SlurmTaskState.RequeueFed:
                case SlurmTaskState.ResvDelHold:
                    {
                        return TaskState.Queued;
                    }

                case SlurmTaskState.Configuring:
                case SlurmTaskState.StageOut:
                case SlurmTaskState.Signaling:
                    {
                        return TaskState.Configuring;
                    }

                case SlurmTaskState.Completed:
                case SlurmTaskState.SpecialExit:
                    {
                        return TaskState.Finished;
                    }

                case SlurmTaskState.Stopped:
                case SlurmTaskState.Canceled:
                case SlurmTaskState.Suspended:
                case SlurmTaskState.Resizing:
                    {
                        return TaskState.Canceled;
                    }

                case SlurmTaskState.Running:
                case SlurmTaskState.Completing:
                    {
                        return TaskState.Running;
                    }

                case SlurmTaskState.Failed:
                case SlurmTaskState.BootFailed:
                case SlurmTaskState.NodeFail:
                case SlurmTaskState.Deadline:
                case SlurmTaskState.Timeout:
                case SlurmTaskState.OutOfMemory:
                case SlurmTaskState.Preempted:
                case SlurmTaskState.Revoked:
                default:
                    {
                        return TaskState.Failed;
                    }
            }
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

        /// <summary>
        /// Change type from object
        /// </summary>
        /// <param name="obj">Value for converting</param>
        /// <param name="type">Type for converting</param>
        /// <returns></returns>
        internal static object ChangeType(object obj, Type type)
        {
            switch (type.Name)
            {
                case "TimeSpan":
                    {
                        string parsedText = Convert.ToString(obj);
                        if (!string.IsNullOrEmpty(parsedText) && TimeSpan.TryParse(parsedText, out TimeSpan timeSpan))
                        {
                            return timeSpan;
                        }
                        else
                        {
                            return new TimeSpan(0);
                        }
                    }
                case "DateTime":
                    {
                        string parsedText = Convert.ToString(obj);
                        if (!string.IsNullOrEmpty(parsedText) && DateTime.TryParse(parsedText, out DateTime date))
                        {
                            return date.Kind == DateTimeKind.Utc
                                ? date
                                : new DateTime(date.Ticks, DateTimeKind.Local).ToUniversalTime();
                        }
                        else
                        {
                            return null;
                        }
                    }
                default:
                    {
                        return Convert.ChangeType(obj, type);
                    }
            }
        }
    }
}
