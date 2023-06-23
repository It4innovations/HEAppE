using HEAppE.DomainObjects.ClusterInformation;
using System.ComponentModel.DataAnnotations.Schema;

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