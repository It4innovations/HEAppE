using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Initialize cluster script directory model
/// </summary>
[DataContract(Name = "InitializeClusterScriptDirectoryModel")]
[Description("Initialize cluster script directory model")]
public class InitializeClusterScriptDirectoryModel : SessionCodeModel
{
    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }

    /// <summary>
    /// Cluster project root directory
    /// </summary>
    [DataMember(Name = "ClusterProjectRootDirectory", IsRequired = true)]
    [Description("Cluster project root directory")]
    public string ClusterProjectRootDirectory { get; set; }

    /// <summary>
    /// Username filter
    /// </summary>
    [DataMember(Name = "Username", IsRequired = false)]
    [Description("Username")]
    public string Username { get; set; }
}