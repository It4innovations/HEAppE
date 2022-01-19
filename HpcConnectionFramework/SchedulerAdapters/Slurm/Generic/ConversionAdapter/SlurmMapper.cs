using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.Enums;
using System;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.ConversionAdapter
{
    /// <summary>
    /// Class: Mapper class
    /// </summary>
    internal static class SlurmMapper
    {
        /// <summary>
        /// Method: Mapping job state from Slurm state
        /// </summary>
        /// <param name="jobState">Job state</param>
        /// <returns></returns>
        internal static JobState MappingJobState(SlurmJobState jobState)
        {
            switch (jobState)
            {
                case SlurmJobState.Requeued:
                case SlurmJobState.Pending:
                case SlurmJobState.RequeueHold:
                case SlurmJobState.RequeueFed:
                case SlurmJobState.ResvDelHold:
                    {
                        return JobState.Queued;
                    }

                case SlurmJobState.Configuring:
                case SlurmJobState.StageOut:
                case SlurmJobState.Signaling:
                    {
                        return JobState.Configuring;
                    }

                case SlurmJobState.Completed:
                case SlurmJobState.SpecialExit:
                    {
                        return JobState.Finished;
                    }

                case SlurmJobState.Stopped:
                case SlurmJobState.Canceled:
                case SlurmJobState.Suspended:
                case SlurmJobState.Resizing:
                    {
                        return JobState.Canceled;
                    }

                case SlurmJobState.Running:
                case SlurmJobState.Completing:
                    {
                        return JobState.Running;
                    }

                case SlurmJobState.Failed:
                case SlurmJobState.BootFailed:
                case SlurmJobState.NodeFail:
                case SlurmJobState.Deadline:
                case SlurmJobState.Timeout:
                case SlurmJobState.OutOfMemory:
                case SlurmJobState.Preempted:
                case SlurmJobState.Revoked:
                default:
                    {
                        return JobState.Failed;
                    }
            }
        }

        /// <summary>
        /// Method: Mapping task state from Slurm state
        /// </summary>
        /// <param name="taskState">Task state</param>
        /// <returns></returns>
        internal static TaskState MappingTaskState(SlurmJobState taskState)
        {
            switch (taskState)
            {
                case SlurmJobState.Requeued:
                case SlurmJobState.Pending:
                case SlurmJobState.RequeueHold:
                case SlurmJobState.RequeueFed:
                case SlurmJobState.ResvDelHold:
                    {
                        return TaskState.Queued;
                    }

                case SlurmJobState.Configuring:
                case SlurmJobState.StageOut:
                case SlurmJobState.Signaling:
                    {
                        return TaskState.Configuring;
                    }

                case SlurmJobState.Completed:
                case SlurmJobState.SpecialExit:
                    {
                        return TaskState.Finished;
                    }

                case SlurmJobState.Stopped:
                case SlurmJobState.Canceled:
                case SlurmJobState.Suspended:
                case SlurmJobState.Resizing:
                    {
                        return TaskState.Canceled;
                    }

                case SlurmJobState.Running:
                case SlurmJobState.Completing:
                    {
                        return TaskState.Running;
                    }

                case SlurmJobState.Failed:
                case SlurmJobState.BootFailed:
                case SlurmJobState.NodeFail:
                case SlurmJobState.Deadline:
                case SlurmJobState.Timeout:
                case SlurmJobState.OutOfMemory:
                case SlurmJobState.Preempted:
                case SlurmJobState.Revoked:
                default:
                    {
                        return TaskState.Failed;
                    }
            }
        }

        /// <summary>
        /// Method: Change type from object
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
