using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DomainObjects.Management
{
    public class ClusterInitReport
    {
        public Cluster Cluster { get; set; }
        public long NumberOfInitializedAccounts { get; set; }
        public long NumberOfNotInitializedAccounts { get; set; }
        public bool IsClusterInitialized => NumberOfNotInitializedAccounts == 0 && NumberOfInitializedAccounts > 0;
    }
}
