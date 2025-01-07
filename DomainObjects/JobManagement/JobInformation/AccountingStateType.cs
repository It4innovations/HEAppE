using System;

namespace HEAppE.DomainObjects.JobManagement.JobInformation;

[Flags]
public enum AccountingStateType
{
    Unknown = 0,
    Queued = 1,
    Running = 2,
    Finished = 4,
    Failed = 8
}