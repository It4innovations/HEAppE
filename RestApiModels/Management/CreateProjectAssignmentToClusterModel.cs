using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Create project assignment to cluster model
/// </summary>
[DataContract(Name = "CreateProjectAssignmentToClusterModel")]
[Description("Create project assignment to cluster model")]
public class CreateProjectAssignmentToClusterModel : SessionCodeModel
{
    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }

    /// <summary>
    /// Cluster id
    /// </summary>
    [DataMember(Name = "ClusterId", IsRequired = true)]
    [Description("Cluster id")]
    public long ClusterId { get; set; }

    /// <summary>
    /// Scratch storage path
    /// </summary>
    [DataMember(Name = "ScratchStoragePath", IsRequired = true)]
    [StringLength(100)]
    [Description("Scratch Storage Path")]
    public string ScratchStoragePath { get; set; }
    
    /// <summary>
    /// Project storage path
    /// </summary>
    [DataMember(Name = "ProjectStoragePath", IsRequired = true)]
    [StringLength(100)]
    [Description("Project Storage Path")]
    public string ProjectStoragePath { get; set; }
}