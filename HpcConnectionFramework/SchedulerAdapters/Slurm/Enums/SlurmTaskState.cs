using System;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Enums
{
    /// <summary>
    /// Slurm states
    /// </summary>
    internal enum SlurmTaskState
    {
        /// <summary>
        /// Job terminated due to launch failure, typically due to a hardware failure 
        /// (e.g. unable to boot the node or block and the job can not be requeued). 
        /// </summary>
        BootFailed = 1,

        /// <summary>
        /// Job was explicitly cancelled by the user or system administrator. The job may or may not have been initiated. 
        /// </summary>
        Canceled = 2,

        /// <summary>
        /// Job has terminated all processes on all nodes with an exit code of zero. 
        /// </summary>
        Completed = 3,

        /// <summary>
        /// Job has been allocated resources, but are waiting for them to become ready for use (e.g. booting). 
        /// </summary>
        Configuring = 4,

        /// <summary>
        /// Job is in the process of completing. Some processes on some nodes may still be active. 
        /// </summary>
        Completing = 5,

        /// <summary>
        /// Job terminated on deadline. 
        /// </summary>
        Deadline = 6,

        /// <summary>
        /// Job terminated with non-zero exit code or other failure condition. 
        /// </summary>
        Failed = 7,

        /// <summary>
        /// Job terminated due to failure of one or more allocated nodes. 
        /// </summary>
        NodeFail = 8,

        /// <summary>
        /// Job experienced out of memory error. 
        /// </summary>
        OutOfMemory = 9,

        /// <summary>
        /// Job is awaiting resource allocation. 
        /// </summary>
        Pending = 10,

        /// <summary>
        /// Job terminated due to preemption.
        /// </summary>
        Preempted = 11,

        /// <summary>
        /// Job currently has an allocation.
        /// </summary>
        Running = 12,

        /// <summary>
        /// Job is held. 
        /// </summary>
        ResvDelHold = 13,

        /// <summary>
        /// Job is being requeued by a federation. 
        /// </summary>
        RequeueFed = 14,

        /// <summary>
        /// Held job is being requeued.
        /// </summary>
        RequeueHold = 15,

        /// <summary>
        /// Completing job is being requeued. 
        /// </summary>
        Requeued = 16,

        /// <summary>
        /// Job is about to change size. 
        /// </summary>
        Resizing = 17,

        /// <summary>
        /// Sibling was removed from cluster due to other cluster starting the job. 
        /// </summary>
        Revoked = 18,

        /// <summary>
        /// Job is being signaled. 
        /// </summary>
        Signaling = 19,

        /// <summary>
        /// The job was requeued in a special state. This state can be set by users, typically in EpilogSlurmctld, if the job has terminated with a particular exit value. 
        /// </summary>
        SpecialExit = 20,

        /// <summary>
        /// Job is staging out files. 
        /// </summary>
        StageOut = 21,

        /// <summary>
        /// Job has an allocation, but execution has been stopped with SIGSTOP signal. CPUS have been retained by this job. 
        /// </summary>
        Stopped = 22,

        /// <summary>
        /// Job has an allocation, but execution has been suspended and CPUs have been released for other jobs. 
        /// </summary>
        Suspended = 23,

        /// <summary>
        /// Job terminated upon reaching its time limit. 
        /// </summary>
        Timeout = 24
    }
}
