using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models
{
    [DataContract(Name = "ClusterNodeTypeResourceUsageExt")]
    public class NodeUsedCoresAndLimitationExt
    {
        public int CoresUsed { get; set; } = 0;
        public ResourceLimitationExt Limitation { get; set; }
        public override string ToString()
        {
            return $"NodeUsedCoresAndLimitationExt: CoresUsed={CoresUsed}, Limitation={Limitation}";
        }
    }
}