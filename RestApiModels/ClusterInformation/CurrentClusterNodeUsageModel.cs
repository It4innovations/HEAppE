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

    }
}
