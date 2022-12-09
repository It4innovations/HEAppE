using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DomainObjects.UserAndLimitationManagement
{
    public class ResourceUsage
    {
        public int CoresUsed { get; set; }
        public ClusterNodeType NodeType { get; set; }
        public ResourceLimitation Limitation { get; set; }
    }
}