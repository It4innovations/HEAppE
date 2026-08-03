using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
using SshCaAPI.Configuration;
using HEAppE.Utils;
using HEAppE.ExternalAuthentication;

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

        string? resolvedUsername = null;
        if (adaptorUserId.HasValue)
        {
            var project = _unitOfWork.ProjectRepository.GetById(projectId);
            if (project != null && project.IsOneToOneMapping)
            {
                var firstNotInit = notInitializedCredentials.FirstOrDefault();
                string? pubKey = null;
                if (firstNotInit != null)
                {
                    if (!string.IsNullOrEmpty(firstNotInit.PublicKey))
                        pubKey = firstNotInit.PublicKey;
                    else if (!string.IsNullOrEmpty(firstNotInit.PrivateKey))
                        pubKey = SSHGenerator.GetPublicKeyFromPrivateKey(firstNotInit).PublicKeyInAuthorizedKeysFormat;
                }

                resolvedUsername = await ResolveUsernameFromContextAsync(adaptorUserId.Value, project, pubKey);
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

            if (!string.IsNullOrEmpty(resolvedUsername) && credential.Username != resolvedUsername)
            {
                _logger.LogInformation($"Synchronizing username for Credential ID {credential.Id} during initialization: {credential.Username} -> {resolvedUsername}");
                credential.Username = resolvedUsername;
                await _unitOfWork.ClusterAuthenticationCredentialsRepository.UpdateAsync(credential);
                anyChanged = true;
            }

            var initProject = clusterProjectCredential.ClusterProject.Project;
            var initCluster = clusterProjectCredential.ClusterProject.Cluster;
            var localBasepath = clusterProjectCredential.ClusterProject.ScratchStoragePath;

            var scheduler = SchedulerFactory
                .GetInstance(initCluster.SchedulerType)
                .CreateScheduler(initCluster, initProject, _sshCertificateAuthorityService, adaptorUserId, _expirioService, _expirioToken, _logger);

            var clusterConfig = ClusterRuntimeConfiguration.For(initCluster.CustomConfiguration);
            string path = Path.Combine(initProject.AccountingString, clusterConfig.InstanceIdentifierPath);
                
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

    private async Task<string?> ResolveUsernameFromContextAsync(long? adaptorUserId, Project? project = null, string? publicKey = null)
    {
        string? username = null;
        _logger.LogWarning($"ResolveUsernameFromContextAsync: Start username resolution. AdaptorUserId: {adaptorUserId}, ProjectId: {project?.Id}");
        
        var firecrestClusterProject = project != null ? _unitOfWork.ClusterProjectRepository.AsQueryable()
            .Include(x => x.Cluster)
            .Where(x => x.ProjectId == project.Id && !x.IsDeleted && x.Cluster != null)
            .FirstOrDefault(x => (x.Cluster.SchedulerType & SchedulerType.FirecRestSlurm) == SchedulerType.FirecRestSlurm) : null;

        if (firecrestClusterProject != null)
        {
            _logger.LogWarning($"ResolveUsernameFromContextAsync: Firecrest cluster detected for project {project.Id} (Cluster: {firecrestClusterProject.Cluster.Name}). Bypassing SSH CA resolution.");
            var token = !string.IsNullOrEmpty(_httpContextKeys.Context.IdpToken) ? _httpContextKeys.Context.IdpToken : _httpContextKeys.Context.LEXISToken;
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var cluster = firecrestClusterProject.Cluster;
                    var customConfig = cluster.CustomConfiguration ?? new Dictionary<string, string>();
                    var credentials = await _expirioService.ExchangeFirecrestCredentialsAsync(token, customConfig, _logger);
                    if (credentials != null && 
                        credentials.TryGetValue("clientId", out var clientIdObj) && 
                        credentials.TryGetValue("clientSecret", out var clientSecretObj))
                    {
                        string clientId = clientIdObj.ToString();
                        string clientSecret = clientSecretObj.ToString();

                        string idpUrl = "";
                        if (cluster.CustomConfiguration != null && cluster.CustomConfiguration.TryGetValue("IdpUrl", out var customIdpUrl))
                        {
                            idpUrl = customIdpUrl;
                        }

                        using var serviceScope = HEAppE.FileTransferFramework.ServiceActivator.GetScope();
                        var tokenService = (HEAppE.Services.FirecRest.IFirecRestTokenService)serviceScope.ServiceProvider.GetService(typeof(HEAppE.Services.FirecRest.IFirecRestTokenService));
                        var httpClientFactory = (System.Net.Http.IHttpClientFactory)serviceScope.ServiceProvider.GetService(typeof(System.Net.Http.IHttpClientFactory));

                        if (tokenService != null && httpClientFactory != null)
                        {
                            var fcToken = await tokenService.GetTokenAsync(clientId, clientSecret, idpUrl);
                            var userinfoUrl = FirecRestUtils.GetUserinfoUrl(cluster);

                            using var userinfoRequest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, userinfoUrl);
                            userinfoRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", fcToken);

                            var httpClient = httpClientFactory.CreateClient("");
                            using var userinfoResponse = await httpClient.SendAsync(userinfoRequest);
                            if (userinfoResponse.IsSuccessStatusCode)
                            {
                                var userinfoContent = await userinfoResponse.Content.ReadAsStringAsync();
                                _logger.LogDebug($"[Firecrest userinfo Response] Success. Content: {userinfoContent}");
                                username = FirecRestUtils.ParseUsernameFromUserinfo(userinfoContent);
                                if (!string.IsNullOrEmpty(username))
                                {
                                    _logger.LogWarning($"ResolveUsernameFromContextAsync: Firecrest resolved username: {username}");
                                }
                            }
                            else
                            {
                                var err = await userinfoResponse.Content.ReadAsStringAsync();
                                _logger.LogWarning($"[Firecrest userinfo] Failed with status {userinfoResponse.StatusCode}: {err}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "FirecREST whoami username resolution failed.");
                }
            }
        }
        else
        {
            // 1. SSH CA resolution
            if (SshCaSettings.UsePosixAccountFromCertificate && !string.IsNullOrEmpty(_httpContextKeys.Context.SshCaToken))
            {
                _logger.LogWarning("ResolveUsernameFromContextAsync: Attempting SSH CA resolution.");
                try {
                    string? resourceName = null;
                    if (project != null)
                    {
                        var cp = _unitOfWork.ClusterProjectRepository.AsQueryable()
                            .Include(x => x.Cluster)
                            .ThenInclude(c => c.FileTransferMethods)
                            .FirstOrDefault(x => x.ProjectId == project.Id && !x.IsDeleted);

                        if (cp?.Cluster != null)
                        {
                            resourceName = !string.IsNullOrEmpty(cp.Cluster.MasterNodeName)
                                ? cp.Cluster.MasterNodeName
                                : (cp.Cluster.FileTransferMethods?.FirstOrDefault()?.ServerHostname ?? cp.Cluster.Name);
                        }
                    }

                    username = await _sshCertificateAuthorityService.GetPosixUsernameAsync(_httpContextKeys.Context.SshCaToken, _logger, publicKey, resourceName);
                    _logger.LogWarning($"ResolveUsernameFromContextAsync: SSH CA resolved username: {username}");
                } catch (Exception ex) {
                    _logger.LogWarning(ex, "SSH CA username resolution failed.");
                }
            }
            
            // 2. Kerberos enriched username resolution
            if (string.IsNullOrEmpty(username))
            {
                bool attemptKerberos = false;
                if (project != null)
                {
                    attemptKerberos = _unitOfWork.ClusterProjectRepository.AsQueryable()
                        .Where(x => x.ProjectId == project.Id && !x.IsDeleted)
                        .Any(x => x.PreferredAuthType == ClusterAuthenticationCredentialsAuthType.Kerberos);
                }
                else if (adaptorUserId != null)
                {
                    attemptKerberos = _unitOfWork.ClusterProjectRepository.AsQueryable()
                        .Where(cp => !cp.IsDeleted && cp.PreferredAuthType == ClusterAuthenticationCredentialsAuthType.Kerberos)
                        .Any(cp => _unitOfWork.AdaptorUserGroupRepository.GetQueryableWithoutFilters()
                            .Where(g => g.ProjectId == cp.ProjectId)
                            .Any(g => g.AdaptorUserUserGroupRoles.Any(r => !r.IsDeleted && r.AdaptorUserId == adaptorUserId)));
                }
                else
                {
                    attemptKerberos = _unitOfWork.ClusterProjectRepository.AsQueryable()
                        .Any(x => !x.IsDeleted && x.PreferredAuthType == ClusterAuthenticationCredentialsAuthType.Kerberos);
                }

                if (attemptKerberos)
                {
                    _logger.LogWarning("ResolveUsernameFromContextAsync: Attempting Kerberos enriched username resolution.");
                    var token = !string.IsNullOrEmpty(_httpContextKeys.Context.IdpToken) ? _httpContextKeys.Context.IdpToken : _httpContextKeys.Context.LEXISToken;
                    if (!string.IsNullOrEmpty(token))
                    {
                        try
                        {
                            username = await _expirioService.GetEnrichedUsernameAsync(token, _logger);
                            _logger.LogWarning($"ResolveUsernameFromContextAsync: Kerberos enriched resolved username: {username}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Kerberos enriched username resolution failed, falling back to JWT.");
                        }
                    }
                }
            }
        }

        // 3. Token preferred_username resolution
        if (string.IsNullOrEmpty(username))
        {
            bool allowJwtResolution = false;
            if (project != null)
            {
                if (project.IsOneToOneMapping)
                {
                    allowJwtResolution = true;
                }
                else
                {
                    allowJwtResolution = _unitOfWork.ClusterProjectRepository.AsQueryable()
                        .Where(x => x.ProjectId == project.Id && !x.IsDeleted)
                        .Any(x => x.PreferredAuthType == ClusterAuthenticationCredentialsAuthType.Kerberos);
                }
            }

            if (allowJwtResolution)
            {
                _logger.LogWarning("ResolveUsernameFromContextAsync: Attempting JWT preferred_username resolution.");
                var token = !string.IsNullOrEmpty(_httpContextKeys.Context.IdpToken) ? _httpContextKeys.Context.IdpToken : _httpContextKeys.Context.LEXISToken;
                if (!string.IsNullOrEmpty(token))
                {
                    try 
                    {
                        var decoded = JwtTokenDecoder.Decode(token);
                        if (!string.IsNullOrEmpty(decoded.PreferedUsername)) {
                            username = decoded.PreferedUsername;
                            _logger.LogWarning($"ResolveUsernameFromContextAsync: JWT resolved username: {username}");
                        } else if (project != null) {
                            username = StringUtils.GenerateUsername(adaptorUserId ?? 0, project.AccountingString);
                            _logger.LogWarning($"ResolveUsernameFromContextAsync: StringUtils.GenerateUsername resolved username: {username}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to decode JWT token for username resolution.");
                    }
                }
            }
        }

        
        _logger.LogWarning($"ResolveUsernameFromContextAsync: End username resolution. Resolved username: {username}");
        return username;
    }

#pragma warning disable IDE1006
    private string _expirioToken
    {
        get => !string.IsNullOrEmpty(_httpContextKeys.Context.LEXISToken) ? _httpContextKeys.Context.LEXISToken : _httpContextKeys.Context.IdpToken;
    }
#pragma warning restore IDE1006
}
