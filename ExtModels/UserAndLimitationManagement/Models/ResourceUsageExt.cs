using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.ExtModels.ClusterInformation.Models;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

[DataContract(Name = "ResourceUsageExt")]
public class ResourceUsageExt
{
    [Required]
    [DataMember(Name = "NodeType")]
    public ClusterNodeTypeExt NodeType { get; set; }

    [DataMember(Name = "CoresUsed")] public int? CoresUsed { get; set; }

    public override string ToString()
    {
        return $"ResourceUsageExt(nodeType={NodeType}; coresUsed={CoresUsed};)";
    }
}