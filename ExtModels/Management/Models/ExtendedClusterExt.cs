using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.ClusterInformation.Models;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Cluster ext
/// </summary>
[DataContract(Name = "ExtendedClusterExt")]
[Description("ExtendedCluster ext")]
public class ExtendedClusterExt
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
    /// Master node name
    /// </summary>
    [DataMember(Name = "MasterNodeName")]
    [Description("Master node name")]
    public string MasterNodeName { get; set; }
    
    /// <summary>
    /// Scheduler type
    /// </summary>
    [DataMember(Name = "SchedulerType")]
    [Description("Scheduler type")]
    public SchedulerTypeExt SchedulerType { get; set; }
    
    /// <summary>
    /// TimeZone
    /// </summary>
    [DataMember(Name = "TimeZone")]
    [Description("Time zone")]
    public string TimeZone { get; set; }
    
    /// <summary>
    /// Port
    /// </summary>
    [DataMember(Name = "Port")]
    [Description("SSH port")]
    public int? Port { get; set; }

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