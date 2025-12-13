using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.BusinessLogicTier.Logic.ClusterInformation;

public interface IClusterInformationLogic
{
    IEnumerable<Cluster> ListAvailableClusters();
    Task<ClusterNodeUsage> GetCurrentClusterNodeUsage(long clusterNodeId, AdaptorUser loggedUser, long projectId);

    Task<IEnumerable<string>> GetCommandTemplateParametersName(long commandTemplateId, long projectId,
        string userScriptPath,
        AdaptorUser loggedUser);

    Task<ClusterAuthenticationCredentials> GetNextAvailableUserCredentials(long clusterId, long projectId,
        bool requireIsInitialized, long? adaptorUserId);
    ClusterNodeType GetClusterNodeTypeById(long clusterNodeTypeId);
    Cluster GetClusterById(long clusterId);
    IEnumerable<ClusterNodeType> ListClusterNodeTypes();
    bool IsUserAvailableToRun(ClusterAuthenticationCredentials user);

    public ClusterAuthenticationCredentials InitializeCredential(ClusterAuthenticationCredentials credential,
        long projectId, long? adaptorUserId);
}