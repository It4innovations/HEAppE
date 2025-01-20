using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.ExtModels.ClusterInformation.Models;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// Resource usage ext
/// </summary>
[DataContract(Name = "ResourceUsageExt")]
[Description("Resource usage ext")]
public class ResourceUsageExt
{
    /// <summary>
    /// Node type
    /// </summary>
    [Required]
    [DataMember(Name = "NodeType")]
    [Description("Node type")]
    public ClusterNodeTypeExt NodeType { get; set; }

    /// <summary>
    /// Number of cores used
    /// </summary>
    [DataMember(Name = "CoresUsed")]
    [Description("Number of cores used")]
    public int? CoresUsed { get; set; }

    public override string ToString()
    {
        return $"ResourceUsageExt(nodeType={NodeType}; coresUsed={CoresUsed};)";
    }
}