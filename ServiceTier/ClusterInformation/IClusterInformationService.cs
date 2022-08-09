using HEAppE.ExtModels.ClusterInformation.Models;
using System.Collections.Generic;

namespace HEAppE.ServiceTier.ClusterInformation
{
    public interface IClusterInformationService
    {
        IEnumerable<ClusterExt> ListAvailableClusters();
        IEnumerable<string> GetCommandTemplateParametersName(long commandTemplateId, string userScriptPath, string sessionCode);
        ClusterNodeUsageExt GetCurrentClusterNodeUsage(long clusterNodeId, string sessionCode);
    }
}