using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.ClusterInformation
{
    /// <summary>
    /// Specification of name specification group in queue 
    /// </summary>
    [Table("ClusterNodeTypeRequestedGroup")]
    public class ClusterNodeTypeRequestedGroup : IdentifiableDbEntity
    {
        #region Properties
        [StringLength(100)]
        public string Name { get; set; }
        #endregion
        #region Override Methods
        public override string ToString()
        {
            return $"ClusterNodeTypeRequestedGroup: Id={Id}, Name={Name}";
        }
        #endregion
    }
}
