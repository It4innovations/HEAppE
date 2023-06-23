using HEAppE.ExtModels.ClusterInformation.Models;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models
{
    [DataContract(Name = "ResourceUsageExt")]
    public class ResourceUsageExt
    {
        [Required]
        [DataMember(Name = "NodeType")]
        public ClusterNodeTypeExt NodeType { get; set; }

        [DataMember(Name = "CoresUsed")]
        public int? CoresUsed { get; set; }

        [DataMember(Name = "Limitation")]
        public ResourceLimitationExt Limitation { get; set; }

        public override string ToString()
        {
            return $"ResourceUsageExt(nodeType={NodeType}; coresUsed={CoresUsed}; limitation={Limitation})";
        }
    }
}
