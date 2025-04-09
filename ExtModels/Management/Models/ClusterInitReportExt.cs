using System.ComponentModel;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Cluster init report ext
/// </summary>
[Description("Cluster init report ext")]
public class ClusterInitReportExt
{
    /// <summary>
    /// Cluster name
    /// </summary>
    [Description("Cluster name")]
    public string ClusterName { get; set; }

    /// <summary>
    /// Is cluster initialized
    /// </summary>
    [Description("Is cluster initialized")]
    public bool IsClusterInitialized { get; set; }
}