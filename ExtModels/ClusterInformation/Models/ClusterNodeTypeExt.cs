using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Cluster node type ext
/// </summary>
[DataContract(Name = "ClusterNodeTypeExt")]
[Description("Cluster node type ext")]
public class ClusterNodeTypeExt
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
    /// Maximum of walltime
    /// </summary>
    [DataMember(Name = "MaxWalltime")]
    [Description("Maximum of walltime")]
    public int? MaxWalltime { get; set; }

    /// <summary>
    /// File transfer id
    /// </summary>
    [DataMember(Name = "FileTransferMethodId")]
    [Description("File transfer id")]
    public long? FileTransferMethodId { get; set; }

    /// <summary>
    /// Array of projects
    /// </summary>
    [DataMember(Name = "Projects")]
    [Description("Array of projects")]
    public ProjectExt[] Projects { get; set; }

    public override string ToString()
    {
        return
            $"ClusterNodeTypeExt(id={Id}; name={Name}; description={Description}; numberOfNodes={NumberOfNodes}; coresPerNode={CoresPerNode}; maxWalltime={MaxWalltime}; fileTransferMethodId={FileTransferMethodId}; projects={Projects})";
    }
}