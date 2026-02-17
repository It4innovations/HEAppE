using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DomainObjects.ClusterInformation;

[Table("ClusterNodeType")]
public class ClusterNodeType : IdentifiableDbEntity, ISoftDeletableEntity
{
    [Required] [StringLength(250)] public string Name { get; set; }

    [Required] [StringLength(250)] public string Description { get; set; }

    public int? NumberOfNodes { get; set; }

    public int CoresPerNode { get; set; }

    [StringLength(250)] public string Queue { get; set; }

    [StringLength(40)] public string QualityOfService { get; set; }

    [StringLength(40)] public string ClusterAllocationName { get; set; }

    public int? MaxWalltime { get; set; }

    public int? MaxNodesPerUser { get; set; }

    public int? MaxNodesPerJob { get; set; }

    [ForeignKey("Cluster")] public long? ClusterId { get; set; }

    public virtual Cluster Cluster { get; set; }

    [ForeignKey("FileTransferMethod")] public long? FileTransferMethodId { get; set; }

    public virtual FileTransferMethod FileTransferMethod { get; set; }

    public virtual List<ClusterNodeTypeRequestedGroup> RequestedNodeGroups { get; set; } = new();

    public virtual List<CommandTemplate> PossibleCommands { get; set; } = new();

    [ForeignKey("ClusterNodeTypeAggregation")]
    public long? ClusterNodeTypeAggregationId { get; set; }

    public virtual ClusterNodeTypeAggregation ClusterNodeTypeAggregation { get; set; }

    [Required] public bool IsDeleted { get; set; } = false;

    public override string ToString()
    {
        return
            $"ClusterNodeType: Id={Id}, Name={Name}, Queue={Queue}, RequestedNodeGroups={string.Join(",", RequestedNodeGroups)}, Cluster={Cluster}";
    }
}