using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DomainObjects.UserAndLimitationManagement
{
    [Table("ResourceLimitation")]
    public class ResourceLimitation : IdentifiableDbEntity
    {
        public int? TotalMaxCores { get; set; }

        public int? MaxCoresPerJob { get; set; }

        public virtual ClusterNodeType NodeType { get; set; }
    }
}