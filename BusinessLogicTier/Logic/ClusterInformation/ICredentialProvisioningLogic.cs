using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.BusinessLogicTier.Logic.ClusterInformation;

/// <summary>
///     Credential provisioning logic
/// </summary>
public interface ICredentialProvisioningLogic
{
    /// <summary>
    ///     Create and initialize missing credentials for a user in a 1:1 project-to-cluster mapping
    /// </summary>
    /// <param name="clusterId"></param>
    /// <param name="projectId"></param>
    /// <param name="adaptorUserId"></param>
    /// <returns></returns>
    Task<IEnumerable<ClusterAuthenticationCredentials>> CreateAndInitializeMissingCredentials(
        long clusterId, 
        long projectId, 
        long adaptorUserId);

    /// <summary>
    ///     Initialize cluster credentials
    /// </summary>
    /// <param name="clusterId"></param>
    /// <param name="projectId"></param>
    /// <param name="adaptorUserId"></param>
    /// <param name="onlyServiceAccounts"></param>
    /// <returns></returns>
    Task<IEnumerable<ClusterAuthenticationCredentials>> InitializeClusterCredentials(
        long clusterId,
        long projectId,
        long? adaptorUserId, 
        bool onlyServiceAccounts);
}
