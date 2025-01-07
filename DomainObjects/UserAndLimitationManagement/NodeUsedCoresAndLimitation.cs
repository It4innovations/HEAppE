using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DomainObjects.UserAndLimitationManagement;

public class NodeUsedCoresAndLimitation
{
    public int CoresUsed { get; set; } = 0;
    public ClusterNodeType NodeType { get; set; }
}