using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DomainObjects.Management;

public class ClusterAccessReport
{
    public Cluster Cluster { get; set; }
    public bool IsClusterAccessible { get; set; }
    
}