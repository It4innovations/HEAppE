using System.ComponentModel;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Cluster account status ext
/// </summary>
[Description("Cluster account status ext")]
public class ClusterAccountStatusExt
{
    /// <summary>
    /// Cluster
    /// </summary>
    [Description("Cluster")]
    public ClusterExt Cluster { get; set; }
    
    /// <summary>
    /// Project
    /// </summary>
    [Description("Project")]
    public ProjectExt Project { get; set; }

    /// <summary>
    /// Is cluster accessible
    /// </summary>
    [Description("Is initialized")]
    public bool IsInitialized { get; set; }
}