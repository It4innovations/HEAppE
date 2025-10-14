using System.ComponentModel;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Cluster access report ext
/// </summary>
[Description("Cluster access report ext")]
public class ClusterAccessReportExt
{
    /// <summary>
    /// Cluster name
    /// </summary>
    [Description("Cluster name")]
    public string ClusterName { get; set; }

    /// <summary>
    /// Is cluster accessible
    /// </summary>
    [Description("Is cluster accessible")]
    public bool IsClusterAccessible { get; set; }
}