using System.Collections.Generic;
using HEAppE.ExtModels.ClusterInformation.Models;

namespace HEAppE.ServiceTier.ClusterInformation;

public interface IClusterInformationService
{
    IEnumerable<ClusterExt> ListAvailableClusters(string sessionCode, string clusterName, string nodeTypeName,
        string projectName,
        string[] accountingString, string commandTemplateName, bool forceRefresh);

    public ClusterClearCacheInfoExt ListAvailableClustersClearCache(string sessionCode);

    IEnumerable<string> RequestCommandTemplateParametersName(long commandTemplateId, long projectId,
        string userScriptPath, string sessionCode);

    ClusterNodeUsageExt GetCurrentClusterNodeUsage(long clusterNodeId, long projectId, string sessionCode);
}