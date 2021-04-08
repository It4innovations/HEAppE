using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.ClusterInformation
{
    [DataContract(Name = "CurrentClusterNodeUsageModel")]
    public class CurrentClusterNodeUsageModel
    {
        [DataMember(Name = "ClusterNodeId")]
        public long ClusterNodeId { get; set; }

        [DataMember(Name = "SessionCode")]
        public string SessionCode { get; set; }
    }
}
