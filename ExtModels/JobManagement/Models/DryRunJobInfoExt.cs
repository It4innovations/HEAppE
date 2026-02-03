using System;

namespace HEAppE.ExtModels.JobManagement.Models;

public class DryRunJobInfoExt
{
    public long JobId { get; set; }
    public DateTime StartTime { get; set; }
    public long Processors { get; set; }
    public string Node { get; set; }
    public string Partition { get; set; }
    
    public string Message { get; set; }
}