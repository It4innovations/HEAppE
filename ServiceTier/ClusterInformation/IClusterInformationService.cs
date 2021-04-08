using HEAppE.ExtModels.ClusterInformation.Models;

namespace HEAppE.ServiceTier.ClusterInformation
{
    public interface IClusterInformationService
    {
        ClusterExt[] ListAvailableClusters();
        ClusterNodeUsageExt GetCurrentClusterNodeUsage(long clusterNodeId, string sessionCode);
    }
}