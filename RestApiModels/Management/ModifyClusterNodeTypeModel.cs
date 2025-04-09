using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Modify cluster node type model
/// </summary>
[DataContract(Name = "ModifyClusterNodeTypeModel")]
[Description("Modify cluster node type model")]
public class ModifyClusterNodeTypeModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name", IsRequired = true)]
    [StringLength(50)]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description", IsRequired = false)]
    [StringLength(200)]
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
    [DataMember(Name = "CoresPerNode", IsRequired = true)]
    [Description("Number of cores per node")]
    public int CoresPerNode { get; set; }

    /// <summary>
    /// Queue
    /// </summary>
    [DataMember(Name = "Queue")]
    [StringLength(30)]
    [Description("Queue")]
    public string Queue { get; set; }

    /// <summary>
    /// Quality of service
    /// </summary>
    [DataMember(Name = "QualityOfService")]
    [StringLength(40)]
    [Description("Quality of service")]
    public string QualityOfService { get; set; }

    /// <summary>
    /// Maximum wall time
    /// </summary>
    [DataMember(Name = "MaxWalltime")]
    [Description("Maximum wall time")]
    public int? MaxWalltime { get; set; }

    /// <summary>
    /// Cluster allocation name
    /// </summary>
    [DataMember(Name = "ClusterAllocationName")]
    [StringLength(40)]
    [Description("Cluster allocation name")]
    public string ClusterAllocationName { get; set; }

    /// <summary>
    /// Cluster id
    /// </summary>
    [DataMember(Name = "ClusterId")]
    [Description("Cluster id")]
    public long? ClusterId { get; set; }

    /// <summary>
    /// File transfer method id
    /// </summary>
    [DataMember(Name = "FileTransferMethodId")]
    [Description("File transfer method id")]
    public long? FileTransferMethodId { get; set; }

    /// <summary>
    /// Cluster node type aggregation id
    /// </summary>
    [DataMember(Name = "ClusterNodeTypeAggregationId")]
    [Description("Cluster node type aggregation id")]
    public long? ClusterNodeTypeAggregationId { get; set; }
}