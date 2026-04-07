using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Cluster project storage path ext
/// </summary>
[DataContract(Name = "ClusterProjectStoragePathExt")]
[Description("Cluster project storage path ext")]
public class ClusterProjectStoragePathExt
{
    /// <summary>
    /// Cluster Id
    /// </summary>
    [DataMember(Name = "ClusterId")]
    [Description("Cluster Id")]
    public long ClusterId { get; set; }

    /// <summary>
    /// Cluster Name
    /// </summary>
    [DataMember(Name = "ClusterName")]
    [Description("Cluster Name")]
    public string ClusterName { get; set; }

    /// <summary>
    /// Scratch storage path
    /// </summary>
    [DataMember(Name = "ScratchStoragePath")]
    [Description("Scratch storage path")]
    public string ScratchStoragePath { get; set; }

    /// <summary>
    /// Project storage path
    /// </summary>
    [DataMember(Name = "ProjectStoragePath")]
    [Description("Project storage path")]
    public string ProjectStoragePath { get; set; }
}
