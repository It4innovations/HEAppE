using System.Collections.Generic;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.BusinessLogicTier.Logic.ClusterInformation
{
    public interface IClusterInformationLogic
    {

        IList<Cluster> ListAvailableClusters();

        ClusterNodeUsage GetCurrentClusterNodeUsage(long clusterNodeId, AdaptorUser loggedUser);

        IEnumerable<string> GetCommandTemplateParametersName(long commandTemplateId, string userScriptPath, AdaptorUser loggedUser);

        ClusterAuthenticationCredentials GetNextAvailableUserCredentials(long clusterId);

        ClusterNodeType GetClusterNodeTypeById(long clusterNodeTypeId);

        Cluster GetClusterById(long clusterId);

        IList<ClusterNodeType> ListClusterNodeTypes();

        bool IsUserAvailableToRun(ClusterAuthenticationCredentials user);
    }
}