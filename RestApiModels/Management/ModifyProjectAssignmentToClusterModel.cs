using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Modify project assignment to cluster model
/// </summary>
[DataContract(Name = "ModifyProjectAssignmentToClusterModel")]
[Description("Modify project assignment to cluster model")]
public class ModifyProjectAssignmentToClusterModel : SessionCodeModel
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
    /// Permanent storage path
    /// </summary>
    [DataMember(Name = "PermanentStoragePath", IsRequired = true)]
    [StringLength(100)]
    [Description("Permanent Storage Path")]
    public string PermanentStoragePath { get; set; }
}