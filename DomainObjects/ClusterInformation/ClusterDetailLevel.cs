namespace HEAppE.DomainObjects.ClusterInformation;

/// <summary>
/// Specifies the level of detail/nesting returned for cluster information.
/// </summary>
public enum ClusterDetailLevel
{
    /// <summary>
    /// Full hierarchy including NodeTypes, Projects, and CommandTemplates.
    /// </summary>
    Full = 0,

    /// <summary>
    /// Clusters only without nested NodeTypes.
    /// </summary>
    ClustersOnly = 1,

    /// <summary>
    /// Clusters with NodeTypes, but without nested Projects and CommandTemplates.
    /// </summary>
    NodeTypes = 2,

    /// <summary>
    /// Clusters with NodeTypes and Projects, but without nested CommandTemplates.
    /// </summary>
    Projects = 3
}
