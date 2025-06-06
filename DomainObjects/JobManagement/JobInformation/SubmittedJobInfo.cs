using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DomainObjects.JobManagement.JobInformation;

[Table("SubmittedJobInfo")]
public class SubmittedJobInfo : IdentifiableDbEntity
{
    [Required] [StringLength(50)] public string Name { get; set; }

    public JobState State { get; set; }

    public DateTime CreationTime { get; set; }

    public DateTime? SubmitTime { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public double? TotalAllocatedTime { get; set; }

    public virtual Project Project { get; set; }

    public virtual AdaptorUser Submitter { get; set; }

    public virtual JobSpecification Specification { get; set; }

    public virtual List<SubmittedTaskInfo> Tasks { get; set; } = new();

    public virtual List<FileTransferTemporaryKey> FileTransferTemporaryKeys { get; set; } = new();
}