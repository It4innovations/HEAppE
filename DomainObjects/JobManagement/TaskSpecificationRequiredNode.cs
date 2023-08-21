using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement
{
    [Table("TaskSpecificationRequiredNode")]
    public class TaskSpecificationRequiredNode : IdentifiableDbEntity
    {
        #region Properties
        [Required]
        [StringLength(40)]
        public string NodeName { get; set; }
        #endregion

        public TaskSpecificationRequiredNode() : base() { }
        public TaskSpecificationRequiredNode(TaskSpecificationRequiredNode taskSpecificationRequiredNode) : base(taskSpecificationRequiredNode)
        {
            NodeName = taskSpecificationRequiredNode.NodeName;
        }

        #region Override Methods
        public override string ToString()
        {
            return $"TaskSpecificationRequiredNode: Id={Id}, NodeName={NodeName}";
        }
        #endregion
    }
}
