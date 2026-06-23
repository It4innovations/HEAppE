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
        string? resolvedUsername = null;
        if (adaptorUserId.HasValue)
        {
            var project = _unitOfWork.ProjectRepository.GetById(projectId);
            resolvedUsername = await ResolveUsernameFromContextAsync(adaptorUserId.Value, project);
        }

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

    private async Task<string?> ResolveUsernameFromContextAsync(long? adaptorUserId, Project? project = null)
    {
        string? username = null;
        
        // 1. SSH CA resolution
        if (SshCaSettings.UsePosixAccountFromCertificate && !string.IsNullOrEmpty(_httpContextKeys.Context.SshCaToken))
        {
            try {
                username = await _sshCertificateAuthorityService.GetPosixUsernameAsync(_httpContextKeys.Context.SshCaToken, _logger);
            } catch (Exception ex) {
                _logger.LogWarning(ex, "SSH CA username resolution failed.");
            }
        }
        
        // 2. Kerberos enriched username resolution or Firecrest whoami resolution
        if (string.IsNullOrEmpty(username))
        {
            var firecrestClusterProject = project != null ? _unitOfWork.ClusterProjectRepository.AsQueryable()
                .Include(x => x.Cluster)
                .Where(x => x.ProjectId == project.Id && !x.IsDeleted && x.Cluster != null)
                .FirstOrDefault(x => x.Cluster.SchedulerType.HasFlag(SchedulerType.FirecRestSlurm)) : null;

            if (firecrestClusterProject == null)
            {
                var token = !string.IsNullOrEmpty(_httpContextKeys.Context.FIPToken) ? _httpContextKeys.Context.FIPToken : _httpContextKeys.Context.LEXISToken;
                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        username = await _expirioService.GetEnrichedUsernameAsync(token, _logger);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Kerberos enriched username resolution failed, falling back to JWT.");
                    }
                }
            }
            else
            {
                _logger.LogInformation("Attempting FirecREST whoami username resolution.");
                var token = !string.IsNullOrEmpty(_httpContextKeys.Context.FIPToken) ? _httpContextKeys.Context.FIPToken : _httpContextKeys.Context.LEXISToken;
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
                                string protocol = cluster.ConnectionProtocol == ClusterConnectionProtocol.Http ? "http" : "https";
                                string firecrestUrl = $"{protocol}://{cluster.MasterNodeName}";
                                var whoamiUrl = $"{firecrestUrl}/utilities/whoami";

                                using var whoamiRequest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, whoamiUrl);
                                whoamiRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", fcToken);
                                whoamiRequest.Headers.Add("X-Machine-Name", cluster.Name);

                                var httpClient = httpClientFactory.CreateClient("");
                                using var whoamiResponse = await httpClient.SendAsync(whoamiRequest);
                                if (whoamiResponse.IsSuccessStatusCode)
                                {
                                    var whoamiContent = await whoamiResponse.Content.ReadAsStringAsync();
                                    _logger.LogDebug($"[Firecrest whoami Response] Success. Content: {whoamiContent}");
                                    
                                    using var doc = System.Text.Json.JsonDocument.Parse(whoamiContent);
                                    if (doc.RootElement.TryGetProperty("username", out var usernameProp) && usernameProp.ValueKind == System.Text.Json.JsonValueKind.String)
                                    {
                                        username = usernameProp.GetString();
                                    }
                                }
                                else
                                {
                                    var err = await whoamiResponse.Content.ReadAsStringAsync();
                                    _logger.LogWarning($"[Firecrest whoami] Failed with status {whoamiResponse.StatusCode}: {err}");
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
        }
        
        // 3. Token preferred_username resolution
        if (string.IsNullOrEmpty(username))
        {
            var token = !string.IsNullOrEmpty(_httpContextKeys.Context.FIPToken) ? _httpContextKeys.Context.FIPToken : _httpContextKeys.Context.LEXISToken;
            if (!string.IsNullOrEmpty(token))
            {
                try 
                {
                    var decoded = JwtTokenDecoder.Decode(token);
                    if (!string.IsNullOrEmpty(decoded.PreferedUsername)) {
                        username = decoded.PreferedUsername;
                    } else if (project != null) {
                        username = StringUtils.GenerateUsername(adaptorUserId ?? 0, project.AccountingString);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to decode JWT token for username resolution.");
                }
            }
        }
        
        // 4. Fallback to AdaptorUser username
        if (string.IsNullOrEmpty(username) && adaptorUserId.HasValue)
        {
            try
            {
                var adaptorUser = await _unitOfWork.AdaptorUserRepository.GetByIdAsync(adaptorUserId.Value);
                if (adaptorUser != null && !string.IsNullOrEmpty(adaptorUser.Username))
                {
                    username = adaptorUser.Username;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve AdaptorUser username for fallback.");
            }
        }
        
        return username;
    }

#pragma warning disable IDE1006
    private string _expirioToken
    {
        get => !string.IsNullOrEmpty(_httpContextKeys.Context.LEXISToken) ? _httpContextKeys.Context.LEXISToken : _httpContextKeys.Context.FIPToken;
    }
#pragma warning restore IDE1006
}
