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
    /// Overwrite existing cluster project root directory
    /// </summary>
    [DataMember(Name = "overwriteExistingProjectRootDirectory")]
    [Description("Overwrite existing cluster project root directory")]
    public bool OverwriteExistingProjectRootDirectory { get; set; } = false;
}