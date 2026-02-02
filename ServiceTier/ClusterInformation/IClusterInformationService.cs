using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.ExtModels.ClusterInformation.Models;

namespace HEAppE.ServiceTier.ClusterInformation;

public interface IClusterInformationService
{
    Task<IEnumerable<ClusterExt>> ListAvailableClusters(string sessionCode, string clusterName, string nodeTypeName,
        string projectName,
        string[] accountingString, string commandTemplateName, bool forceRefresh);

    public ClusterClearCacheInfoExt ListAvailableClustersClearCache(string sessionCode);

    Task<IEnumerable<string>> RequestCommandTemplateParametersName(long commandTemplateId, long projectId,
        string userScriptPath, string sessionCode);

    Task<ClusterNodeUsageExt> GetCurrentClusterNodeUsage(long clusterNodeId, long projectId, string sessionCode);
}