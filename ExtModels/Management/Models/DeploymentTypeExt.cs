using System.ComponentModel;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Deployment types
/// </summary>
[Description("Deployment types")]
public enum DeploymentTypeExt
{
    Unspecific = 1,
    Docker = 2,
    Kubernetes = 3
}