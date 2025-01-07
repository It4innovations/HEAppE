namespace HEAppE.ExtModels.JobManagement.Models;

public enum TaskStateExt
{
    Configuring = 1,
    Submitted = 2,
    Queued = 4,
    Running = 8,
    Finished = 16,
    Failed = 32,
    Canceled = 64,
    Deleted = 256
}