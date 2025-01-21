using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Cluster node usage ext
/// </summary>
[DataContract(Name = "ClusterNodeUsageExt")]
[Description("Cluster node usage ext")]
public class ClusterNodeUsageExt
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
    /// Priority
    /// </summary>
    [DataMember(Name = "Priority")]
    [Description("Priority")]
    public int? Priority { get; set; }

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
    /// Number of nodes
    /// </summary>
    [DataMember(Name = "NumberOfNodes")]
    [Description("Number of nodes")]
    public int? NumberOfNodes { get; set; }

    /// <summary>
    /// Number of used nodes
    /// </summary>
    [DataMember(Name = "NumberOfUsedNodes")]
    [Description("Number of used nodes")]
    public int? NumberOfUsedNodes { get; set; }

    /// <summary>
    /// Total jobs number
    /// </summary>
    [DataMember(Name = "TotalJobs")]
    [Description("Total jobs number")]
    public int? TotalJobs { get; set; }

    public override string ToString()
    {
        return $"ClusterNodeUsageExt(id={Id}; name={Name}; description={Description}; priority={Priority}; coresPerNode={CoresPerNode}; maxWallTime={MaxWalltime}; numberOfNodes={NumberOfNodes}; numberOfUsedNodes={NumberOfUsedNodes}; totalJobs={TotalJobs})";
    }
}