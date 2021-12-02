using HEAppE.ExtModels.ClusterInformation.Models;
using System.Collections.Generic;

namespace HEAppE.ServiceTier.ClusterInformation
{
    public interface IClusterInformationService
    {
        ClusterExt[] ListAvailableClusters();

        ClusterNodeUsageExt GetCurrentClusterNodeUsage(long clusterNodeId, string sessionCode);

        IEnumerable<string> GetCommandTemplateParametersName(long commandTemplateId, string userScriptPath, string sessionCode);
    }
}