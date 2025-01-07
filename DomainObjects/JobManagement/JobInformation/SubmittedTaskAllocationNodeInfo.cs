using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement.JobInformation;

[Table("SubmittedTaskAllocationNodeInfo")]
public class SubmittedTaskAllocationNodeInfo : IdentifiableDbEntity
{
    #region Override Methods

    public override string ToString()
    {
        return $"SubmittedTaskAllocationNodeInfo: Id={Id}, AllocationNodeId={AllocationNodeId}";
    }

    #endregion

    #region Properties

    [Required] [StringLength(50)] public string AllocationNodeId { get; set; }

    [ForeignKey("SubmittedTask")] public long SubmittedTaskInfoId { get; set; }

    public virtual SubmittedTaskInfo SubmittedTask { get; set; }

    #endregion
}