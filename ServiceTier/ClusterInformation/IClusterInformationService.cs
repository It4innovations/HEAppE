using HEAppE.ExtModels.ClusterInformation.Models;
using System.Collections.Generic;

namespace HEAppE.ServiceTier.ClusterInformation
{
    public interface IClusterInformationService
    {
        IEnumerable<ClusterExt> ListAvailableClusters(string clusterName, string nodeTypeName, string projectName, string accountingString, string commandTemplateName);
        IEnumerable<string> RequestCommandTemplateParametersName(long commandTemplateId, long projectId, string userScriptPath, string sessionCode);
        ClusterNodeUsageExt GetCurrentClusterNodeUsage(long clusterNodeId, long projectId, string sessionCode);
    }
}