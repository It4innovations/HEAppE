using System.Collections.Generic;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.BusinessLogicTier.Logic.ClusterInformation;

public interface IClusterInformationLogic
{
    IEnumerable<Cluster> ListAvailableClusters();
    ClusterNodeUsage GetCurrentClusterNodeUsage(long clusterNodeId, AdaptorUser loggedUser, long projectId);

    IEnumerable<string> GetCommandTemplateParametersName(long commandTemplateId, long projectId, string userScriptPath,
        AdaptorUser loggedUser);

    ClusterAuthenticationCredentials GetNextAvailableUserCredentials(long clusterId, long projectId, bool requireIsInitialized, long? adaptorUserId);
    ClusterNodeType GetClusterNodeTypeById(long clusterNodeTypeId);
    Cluster GetClusterById(long clusterId);
    IEnumerable<ClusterNodeType> ListClusterNodeTypes();
    bool IsUserAvailableToRun(ClusterAuthenticationCredentials user);
}