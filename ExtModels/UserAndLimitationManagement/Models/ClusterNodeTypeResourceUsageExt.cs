using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// Cluster node type resource usage ext
/// </summary>
[DataContract(Name = "ClusterNodeTypeResourceUsageExt")]
[Description("Cluster node type resource usage ext")]
public class ClusterNodeTypeResourceUsageExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long? Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name")]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description")]
    [Description("Description")]
    public string Description { get; set; }
    
    /// <summary>
    /// Number of nodes
    /// </summary>
    [DataMember(Name = "NumberOfNodes")]
    [Description("Number of nodes")]
    public int? NumberOfNodes { get; set; }

    /// <summary>
    /// Number of cores per node
    /// </summary>
    [DataMember(Name = "CoresPerNode")]
    [Description("Number of cores per node")]
    public int? CoresPerNode { get; set; }

    /// <summary>
    /// Maximum walltime
    /// </summary>
    [DataMember(Name = "MaxWalltime")]
    [Description("Maximum walltime")]
    public int? MaxWalltime { get; set; }

    /// <summary>
    /// File transfer method id
    /// </summary>
    [DataMember(Name = "FileTransferMethodId")]
    [Description("File transfer method id")]
    public long? FileTransferMethodId { get; set; }

    /// <summary>
    /// Used cores and limitation of node
    /// </summary>
    [DataMember(Name = "NodeUsedCoresAndLimitation")]
    [Description("Used cores and limitation of node")]
    public NodeUsedCoresAndLimitationExt NodeUsedCoresAndLimitation { get; set; }

    #region Public methods

    public override string ToString()
    {
        return
            $"ReportingClusterNodeTypeExt: Id={Id}, Name={Name}, Description={Description}, numberOfNodes={NumberOfNodes}, coresPerNode={CoresPerNode}, MaxWalltime={MaxWalltime}, FileTransferMethodId={FileTransferMethodId}, NodeUsedCoresAndLimitation={NodeUsedCoresAndLimitation}";
    }

    #endregion
}