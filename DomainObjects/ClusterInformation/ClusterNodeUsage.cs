using System;

namespace HEAppE.DomainObjects.ClusterInformation
{
    public class ClusterNodeUsage
    {
        public ClusterNodeType NodeType { get; set; }

        public int? NodesUsed { get; set; }

        public int? Priority { get; set; }

        public int? TotalJobs { get; set; }
    }
}