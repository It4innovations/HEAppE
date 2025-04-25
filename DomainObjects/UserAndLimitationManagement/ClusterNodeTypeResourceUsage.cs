using System.ComponentModel.DataAnnotations;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DomainObjects.ClusterInformation;

public class ClusterNodeTypeResourceUsage
{
    public long Id { get; set; }

    [Required] [StringLength(50)] public string Name { get; set; }

    [Required] [StringLength(200)] public string Description { get; set; }

    public int? NumberOfNodes { get; set; }

    public int CoresPerNode { get; set; }

    [StringLength(30)] public string Queue { get; set; }

    [StringLength(40)] public string ClusterAllocationName { get; set; }

    public int? MaxWalltime { get; set; }
    public Cluster Cluster { get; set; }
    public FileTransferMethod FileTransferMethod { get; set; }

    public NodeUsedCoresAndLimitation NodeUsedCoresAndLimitation { get; set; }
}