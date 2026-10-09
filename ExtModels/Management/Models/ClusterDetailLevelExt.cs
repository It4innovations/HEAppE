using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Specifies the level of detail/nesting returned for cluster information.
/// </summary>
[DataContract(Name = "ClusterDetailLevelExt")]
[Description("Specifies the level of detail/nesting returned for cluster information")]
public enum ClusterDetailLevelExt
{
    /// <summary>
    /// Full hierarchy including NodeTypes, Projects, and CommandTemplates.
    /// </summary>
    [EnumMember]
    [Description("Full hierarchy including NodeTypes, Projects, and CommandTemplates")]
    Full = 0,

    /// <summary>
    /// Clusters only without nested NodeTypes.
    /// </summary>
    [EnumMember]
    [Description("Clusters only without nested NodeTypes")]
    ClustersOnly = 1,

    /// <summary>
    /// Clusters with NodeTypes, but without nested Projects and CommandTemplates.
    /// </summary>
    [EnumMember]
    [Description("Clusters with NodeTypes, but without nested Projects and CommandTemplates")]
    NodeTypes = 2,

    /// <summary>
    /// Clusters with NodeTypes and Projects, but without nested CommandTemplates.
    /// </summary>
    [EnumMember]
    [Description("Clusters with NodeTypes and Projects, but without nested CommandTemplates")]
    Projects = 3
}
