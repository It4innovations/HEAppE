using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DomainObjects.JobManagement;

public class DryRunJobSpecification
{
    public Project Project { get; set; }
    public ClusterNodeType ClusterNodeType { get; set; }
    public long Nodes { get; set; }
    public long TasksPerNode { get; set; }
    public long WallTimeInMinutes { get; set; }
    public ClusterAuthenticationCredentials ClusterUser { get; set; }
    
    public bool IsGpuPartition { get; set; }
}