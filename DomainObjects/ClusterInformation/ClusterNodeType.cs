using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.ClusterInformation
{
    [Table("ClusterNodeType")]
    public class ClusterNodeType : IdentifiableDbEntity
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }

        public int? NumberOfNodes { get; set; }

        public int CoresPerNode { get; set; }

        [StringLength(30)]
        public string Queue { get; set; }

        [StringLength(40)]
        public string QualityOfService { get; set; }

        [StringLength(40)]
        public string ClusterAllocationName { get; set; }

        public int? MaxWalltime { get; set; }

        public int? MaxNodesPerUser { get; set; }

        public int? MaxNodesPerJob { get; set; }

        [ForeignKey("Cluster")]
        public long? ClusterId { get; set; }
        public virtual Cluster Cluster { get; set; }

        [ForeignKey("FileTransferMethod")]
        public long? FileTransferMethodId { get; set; }
        public virtual FileTransferMethod FileTransferMethod { get; set; }

        public virtual List<ClusterNodeTypeRequestedGroup> RequestedNodeGroups { get; set; } = new List<ClusterNodeTypeRequestedGroup>();

        public virtual List<CommandTemplate> PossibleCommands { get; set; } = new List<CommandTemplate>();

        public override string ToString()
        {
            return $"ClusterNodeType: Id={Id}, Name={Name}, Queue={Queue}, RequestedNodeGroups={string.Join(",", RequestedNodeGroups)}, Cluster={Cluster}";
        }
    }
}