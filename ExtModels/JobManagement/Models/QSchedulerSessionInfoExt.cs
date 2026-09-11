using System;

namespace HEAppE.ExtModels.JobManagement.Models;

public class QSchedulerSessionInfoExt
{
    public long SessionId { get; set; }
    public long ClusterId { get; set; }
    public long ProjectId { get; set; }
    public string State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}
