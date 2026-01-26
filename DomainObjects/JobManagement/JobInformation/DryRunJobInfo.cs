using System;

namespace HEAppE.DomainObjects.JobManagement.JobInformation;

public class DryRunJobInfo
{
    public long JobId { get; set; }
    public DateTime StartTime { get; set; }
    public long Processors { get; set; }
    public string Node { get; set; }
    public string Partition { get; set; }
    
    public string Message { get; set; }
}