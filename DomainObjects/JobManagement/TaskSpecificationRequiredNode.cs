using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement;

[Table("TaskSpecificationRequiredNode")]
public class TaskSpecificationRequiredNode : IdentifiableDbEntity
{
    public TaskSpecificationRequiredNode()
    {
    }

    public TaskSpecificationRequiredNode(TaskSpecificationRequiredNode taskSpecificationRequiredNode) : base(
        taskSpecificationRequiredNode)
    {
        NodeName = taskSpecificationRequiredNode.NodeName;
    }

    #region Properties

    [Required] [StringLength(40)] public string NodeName { get; set; }

    #endregion

    #region Override Methods

    public override string ToString()
    {
        return $"TaskSpecificationRequiredNode: Id={Id}, NodeName={NodeName}";
    }

    #endregion
}