using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement;

[Table("ProjectContact")]
public class ProjectContact
{
    public long ProjectId { get; set; }
    public virtual Project Project { get; set; }

    public long ContactId { get; set; }
    public virtual Contact Contact { get; set; }

    [Required] public bool IsPI { get; set; } = false;

    public override string ToString()
    {
        return $"ProjectContact: Project={Project}, Contact={Contact}, IsPI={IsPI}";
    }
}