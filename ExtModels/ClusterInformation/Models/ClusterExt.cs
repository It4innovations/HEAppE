using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Cluster ext
/// </summary>
[DataContract(Name = "ClusterExt")]
[Description("Cluster ext")]
public class ClusterExt
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
    /// Array of node types
    /// </summary>
    [DataMember(Name = "NodeTypes")]
    [Description("Array of node types")]
    public ClusterNodeTypeExt[] NodeTypes { get; set; }

    public override string ToString()
    {
        return $"ClusterInfoExt(Id={Id}; Name={Name}; Description={Description}; NodeTypes={NodeTypes})";
    }
}