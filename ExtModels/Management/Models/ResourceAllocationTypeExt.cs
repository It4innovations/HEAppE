using System.ComponentModel;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Resource allocation types
/// </summary>
[Description("Resource allocation types")]
public enum ResourceAllocationTypeExt
{
    None = 1,
    HPC = 2,
    Cloud = 3
}