using HEAppE.DomainObjects.ClusterInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DomainObjects.UserAndLimitationManagement
{
    public class NodeUsedCoresAndLimitation
    {
        public int CoresUsed { get; set; } = 0;
        public ClusterNodeType NodeType { get; set; }
        public ResourceLimitation Limitation { get; set; }
    }
}
