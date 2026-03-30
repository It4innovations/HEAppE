using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.CertificateGenerator;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.Exceptions.External;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.Services.Expirio;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier.Logic.ClusterInformation;

/// <summary>
///     Credential provisioning logic implementation
/// </summary>
public class CredentialProvisioningLogic : ICredentialProvisioningLogic
{
    private readonly ILogger _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IExpirioService _expirioService;

    public CredentialProvisioningLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys, IExpirioService expirioService, ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _expirioService = expirioService;
        _logger = logger;
    }

    public async Task<IEnumerable<ClusterAuthenticationCredentials>> CreateAndInitializeMissingCredentials(
        long clusterId, 
        long projectId, 
        long adaptorUserId)
    {
        _logger.LogInformation("Creating missing credentials for ClusterId: {0}, ProjectId: {1}, AdaptorUser: {2}", clusterId, projectId, adaptorUserId);

        var project = _unitOfWork.ProjectRepository.GetByIdWithClusterProjects(projectId);
        var clusterProject = project?.ClusterProjects?.FirstOrDefault(cp => cp.ClusterId == clusterId);
        var cluster = _unitOfWork.ClusterRepository.GetById(clusterId) ?? throw new RequestedObjectDoesNotExistException("ClusterNotExists", clusterId);
        
        var preferredAuthType = clusterProject?.PreferredAuthType ?? ClusterAuthenticationCredentialsAuthType.SshCertificate;

        // Resolve username
        string username = $"account_{projectId}_{adaptorUserId}";
        var adaptorUser = _unitOfWork.AdaptorUserRepository.GetById(adaptorUserId);
        if (adaptorUser != null)
        {
            username = adaptorUser.Username;
        }

        // Try to resolve "real" username via SSH CA if requested
        if (preferredAuthType == ClusterAuthenticationCredentialsAuthType.SshCertificate || preferredAuthType == ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy)
        {
            username = await ResolveUsernameViaSshCa(cluster, _httpContextKeys.Context.SshCaToken, username);
        }

        var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
        
        switch (preferredAuthType)
        {
            case ClusterAuthenticationCredentialsAuthType.SshCertificate:
            case ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy:
            case ClusterAuthenticationCredentialsAuthType.PrivateKey:
            case ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKey:
                await managementLogic.CreateSecureShellKey(
                    credentials: new List<(string, string)> { (username, string.Empty) },
                    projectId: projectId,
                    adaptorUserId: adaptorUserId);
                break;
            default:
                _logger.LogWarning("Automated provisioning for AuthType {0} is not fully implemented or requires manual setup. Falling back to SSH key generation.", preferredAuthType);
                await managementLogic.CreateSecureShellKey(
                    credentials: new List<(string, string)> { (username, string.Empty) },
                    projectId: projectId,
                    adaptorUserId: adaptorUserId);
                break;
        }
        
        return await InitializeClusterCredentials(
            clusterId: clusterId, 
            projectId: projectId, 
            adaptorUserId: adaptorUserId, 
            onlyServiceAccounts: false); 
    }

    public async Task<IEnumerable<ClusterAuthenticationCredentials>> InitializeClusterCredentials(
        long clusterId,
        long projectId,
        long? adaptorUserId, 
        bool onlyServiceAccounts)
    {
        var initializedCredentials = new List<ClusterAuthenticationCredentials>();
        List<ClusterAuthenticationCredentials> notInitializedCredentials = new List<ClusterAuthenticationCredentials>();
        
        if (onlyServiceAccounts)
        {
            var serviceAccount = await _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(clusterId, projectId, false, adaptorUserId, _logger)
                                 ?? throw new RequestedObjectDoesNotExistException("ClusterAuthenticationCredentialsNoServiceAccount", clusterId, projectId, adaptorUserId);
            notInitializedCredentials.Add(serviceAccount);
        }
        else
        {
            var serviceAccount = await _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(clusterId, projectId, false, adaptorUserId, _logger);
            var credentials = (await _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAuthenticationCredentialsForClusterAndProject(clusterId, projectId, false, adaptorUserId, _logger)).ToList();
            
            notInitializedCredentials.AddRange(credentials);
            
            if (serviceAccount != null && notInitializedCredentials.All(c => c.Id != serviceAccount.Id))
            {
                notInitializedCredentials.Add(serviceAccount);
            }
        }
        
        // This check is now delegated to the caller if needed, but for internal consistency we can keep a simpler version here if it's strictly about initialization
        // but wait, if we move it here, we should be careful about recursion.
        // The original logic in ClusterInformationLogic called CreateAndInitializeMissingCredentials from InitializeClusterCredentials.
        
        bool anyChanged = false;
        foreach (var credential in notInitializedCredentials)
        {
            if (credential == null) continue;

            var clusterProjectCredential = credential.ClusterProjectCredentials?.FirstOrDefault(cpc =>
                cpc.ClusterProject.ProjectId == projectId && cpc.ClusterProject.ClusterId == clusterId);

            if (clusterProjectCredential == null)
            {
                _logger.LogWarning($"ClusterProjectCombinationNotFound for ClusterId: {clusterId}, ProjectId: {projectId}, User: {credential.Username}");
                continue;
            }

            var initProject = clusterProjectCredential.ClusterProject.Project;
            var initCluster = clusterProjectCredential.ClusterProject.Cluster;
            var localBasepath = clusterProjectCredential.ClusterProject.ScratchStoragePath;

            var scheduler = SchedulerFactory
                .GetInstance(initCluster.SchedulerType)
                .CreateScheduler(initCluster, initProject, _sshCertificateAuthorityService, adaptorUserId, _expirioService, _logger);

            string path = Path.Combine(initProject.AccountingString,
                HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath);
                
            var isInitialized = scheduler.InitializeClusterScriptDirectory(
                path,
                true,
                localBasepath,
                initCluster,
                credential,
                clusterProjectCredential.IsServiceAccount,
                _httpContextKeys.Context.SshCaToken, 
                _httpContextKeys.Context.LEXISToken);

            if (isInitialized)
            {
                clusterProjectCredential.IsInitialized = true;
                initializedCredentials.Add(credential);
                anyChanged = true;
            }
        }

        if (anyChanged)
        {
            _unitOfWork.Save();
        }

        return initializedCredentials;
    }

    private async Task<string> ResolveUsernameViaSshCa(Cluster cluster, string sshCaToken, string fallbackUsername)
    {
        if (string.IsNullOrEmpty(sshCaToken)) return fallbackUsername;

        try
        {
            _logger.LogDebug("Attempting to resolve real POSIX username via SSH CA for cluster {0}", cluster.Name);
            var sshGenerator = new SSHGenerator(_logger);
            var tempKey = sshGenerator.GetEncryptedSecureShellKey(fallbackUsername, string.Empty);
            
            var response = await _sshCertificateAuthorityService.SignAsync(tempKey.PublicKeyInAuthorizedKeysFormat, sshCaToken, cluster.MasterNodeName, _logger);
            if (response != null && !string.IsNullOrEmpty(response.PosixUsername))
            {
                _logger.LogInformation("Resolved real POSIX username '{0}' via SSH CA for cluster {1}", response.PosixUsername, cluster.Name);
                return response.PosixUsername;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve username via SSH CA. Falling back to '{0}'.", fallbackUsername);
        }

        return fallbackUsername;
    }
}
