using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Cluster node type for task ext
/// </summary>
[DataContract(Name = "ClusterNodeTypeForTaskExt")]
[Description("Cluster node type for task ext")]
public class ClusterNodeTypeForTaskExt
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
    /// Project
    /// </summary>
    [DataMember(Name = "Projects")]
    [Description("Project")]
    public ProjectForTaskExt Project { get; set; }

    public override string ToString()
    {
        return $"ClusterNodeTypeExt(id={Id}; name={Name}; description={Description}; project={Project})";
    }
}