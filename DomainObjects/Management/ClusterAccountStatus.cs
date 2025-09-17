using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DomainObjects.Management;

public class ClusterAccountStatus
{
    public Cluster Cluster { get; set; }
    public Project Project { get; set; }
    public bool IsInitialized { get; set; }
    
}