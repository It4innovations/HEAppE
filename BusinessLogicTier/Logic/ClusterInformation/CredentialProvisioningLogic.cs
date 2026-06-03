using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.Management;
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

        var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
        
        // Use the new unified CreateCredential with nulls to trigger auto-resolution
        await managementLogic.CreateCredential(
            username: null, 
            password: null, 
            authType: null, 
            generateNewKey: null, 
            privateKey: null, 
            passphrase: null, 
            projectId: projectId, 
            adaptorUserId: adaptorUserId);
        
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
                .CreateScheduler(initCluster, initProject, _sshCertificateAuthorityService, adaptorUserId, _expirioService, _expirioToken, _logger);

            string path = Path.Combine(initProject.AccountingString,
                HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath);
                
            var isInitialized = await scheduler.InitializeClusterScriptDirectoryAsync(
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

#pragma warning disable IDE1006
    private string _expirioToken

    {
        get => !string.IsNullOrEmpty(_httpContextKeys.Context.LEXISToken) ? _httpContextKeys.Context.LEXISToken : _httpContextKeys.Context.FIPToken;
    }
#pragma warning restore IDE1006
}
