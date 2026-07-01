using System;

namespace HEAppE.DomainObjects.JobManagement.JobInformation;

[Flags]
public enum TaskState
{
    Unknown = 0,
    Configuring = 1,
    Submitted = 2,
    Queued = 4,
    Running = 8,
    Finished = 16,
    Failed = 32,
    Canceled = 64,
    Paused = 128,
    Deleted = 256
}