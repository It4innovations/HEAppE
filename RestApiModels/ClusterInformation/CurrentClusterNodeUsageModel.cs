using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.ClusterInformation
{
    [DataContract(Name = "CurrentClusterNodeUsageModel")]
    public class CurrentClusterNodeUsageModel : SessionCodeModel
    {
        [DataMember(Name = "ClusterNodeId")]
        public long ClusterNodeId { get; set; }
        [DataMember(Name = "ProjectId", IsRequired = true)]
        public long ProjectId { get; set; }
        public override string ToString()
        {
            return $"CurrentClusterNodeUsageModel({base.ToString()}; ClusterNodeId: {ClusterNodeId})";
        }

    }
}
