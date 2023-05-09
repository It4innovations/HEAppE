using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System.Collections.Generic;

namespace HEAppE.BusinessLogicTier.Logic.ClusterInformation
{
    public interface IClusterInformationLogic
    {
        IEnumerable<Cluster> ListAvailableClusters();
        ClusterNodeUsage GetCurrentClusterNodeUsage(long clusterNodeId, AdaptorUser loggedUser);
        IEnumerable<string> GetCommandTemplateParametersName(long commandTemplateId, long projectId, string userScriptPath, AdaptorUser loggedUser);
        ClusterAuthenticationCredentials GetNextAvailableUserCredentials(long clusterId, long projectId);
        ClusterNodeType GetClusterNodeTypeById(long clusterNodeTypeId);
        Cluster GetClusterById(long clusterId);
        IEnumerable<ClusterNodeType> ListClusterNodeTypes();
        bool IsUserAvailableToRun(ClusterAuthenticationCredentials user);
    }
}