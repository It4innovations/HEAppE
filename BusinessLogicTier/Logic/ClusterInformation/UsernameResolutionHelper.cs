using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.ExternalAuthentication;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.Services.Expirio;
using HEAppE.Utils;
using Microsoft.Extensions.Logging;
using SshCaAPI;
using SshCaAPI.Configuration;

namespace HEAppE.BusinessLogicTier.Logic.ClusterInformation;

/// <summary>
///     Unified helper for resolving cluster username from context (SSH CA, Firecrest, Kerberos, JWT)
/// </summary>
public static class UsernameResolutionHelper
{
    public static async Task<string?> ResolveUsernameFromContextAsync(
        IUnitOfWork unitOfWork,
        ISshCertificateAuthorityService sshCertificateAuthorityService,
        IHttpContextKeys httpContextKeys,
        IExpirioService expirioService,
        ILogger logger,
        long? adaptorUserId,
        Project? project = null,
        string? publicKey = null)
    {
        string? username = null;
        logger.LogWarning($"ResolveUsernameFromContextAsync: Start username resolution. AdaptorUserId: {adaptorUserId}, ProjectId: {project?.Id}");
        
        var firecrestClusterProject = project != null ? unitOfWork.ClusterProjectRepository.AsQueryable()
            .Include(x => x.Cluster)
            .Where(x => x.ProjectId == project.Id && !x.IsDeleted && x.Cluster != null)
            .FirstOrDefault(x => (x.Cluster.SchedulerType & SchedulerType.FirecRestSlurm) == SchedulerType.FirecRestSlurm) : null;

        if (firecrestClusterProject != null)
        {
            logger.LogWarning($"ResolveUsernameFromContextAsync: Firecrest cluster detected for project {project.Id} (Cluster: {firecrestClusterProject.Cluster.Name}). Bypassing SSH CA resolution.");
            var token = !string.IsNullOrEmpty(httpContextKeys.Context.IdpToken) ? httpContextKeys.Context.IdpToken : httpContextKeys.Context.LEXISToken;
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var cluster = firecrestClusterProject.Cluster;
                    var customConfig = cluster.CustomConfiguration ?? new Dictionary<string, string>();
                    var credentials = await expirioService.ExchangeFirecrestCredentialsAsync(token, customConfig, logger);
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
                                logger.LogDebug($"[Firecrest userinfo Response] Success. Content: {userinfoContent}");
                                username = FirecRestUtils.ParseUsernameFromUserinfo(userinfoContent);
                                if (!string.IsNullOrEmpty(username))
                                {
                                    logger.LogWarning($"ResolveUsernameFromContextAsync: Firecrest resolved username: {username}");
                                }
                            }
                            else
                            {
                                var err = await userinfoResponse.Content.ReadAsStringAsync();
                                logger.LogWarning($"[Firecrest userinfo] Failed with status {userinfoResponse.StatusCode}: {err}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "FirecREST whoami username resolution failed.");
                }
            }
        }
        else
        {
            // 1. SSH CA resolution
            if (SshCaSettings.UsePosixAccountFromCertificate && !string.IsNullOrEmpty(httpContextKeys.Context.SshCaToken))
            {
                logger.LogWarning("ResolveUsernameFromContextAsync: Attempting SSH CA resolution.");
                try {
                    string? resourceName = null;
                    if (project != null)
                    {
                        var cp = unitOfWork.ClusterProjectRepository.AsQueryable()
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

                    username = await sshCertificateAuthorityService.GetPosixUsernameAsync(httpContextKeys.Context.SshCaToken, logger, publicKey, resourceName);
                    logger.LogWarning($"ResolveUsernameFromContextAsync: SSH CA resolved username: {username}");
                } catch (Exception ex) {
                    logger.LogWarning(ex, "SSH CA username resolution failed.");
                }
            }
            
            // 2. Kerberos enriched username resolution
            if (string.IsNullOrEmpty(username))
            {
                bool attemptKerberos = false;
                if (project != null)
                {
                    attemptKerberos = unitOfWork.ClusterProjectRepository.AsQueryable()
                        .Where(x => x.ProjectId == project.Id && !x.IsDeleted)
                        .Any(x => x.PreferredAuthType == ClusterAuthenticationCredentialsAuthType.Kerberos);
                }
                else if (adaptorUserId != null)
                {
                    attemptKerberos = unitOfWork.ClusterProjectRepository.AsQueryable()
                        .Where(cp => !cp.IsDeleted && cp.PreferredAuthType == ClusterAuthenticationCredentialsAuthType.Kerberos)
                        .Any(cp => unitOfWork.AdaptorUserGroupRepository.GetQueryableWithoutFilters()
                            .Where(g => g.ProjectId == cp.ProjectId)
                            .Any(g => g.AdaptorUserUserGroupRoles.Any(r => !r.IsDeleted && r.AdaptorUserId == adaptorUserId)));
                }
                else
                {
                    attemptKerberos = unitOfWork.ClusterProjectRepository.AsQueryable()
                        .Any(x => !x.IsDeleted && x.PreferredAuthType == ClusterAuthenticationCredentialsAuthType.Kerberos);
                }

                if (attemptKerberos)
                {
                    logger.LogWarning("ResolveUsernameFromContextAsync: Attempting Kerberos enriched username resolution.");
                    var token = !string.IsNullOrEmpty(httpContextKeys.Context.IdpToken) ? httpContextKeys.Context.IdpToken : httpContextKeys.Context.LEXISToken;
                    if (!string.IsNullOrEmpty(token))
                    {
                        try
                        {
                            username = await expirioService.GetEnrichedUsernameAsync(token, logger);
                            logger.LogWarning($"ResolveUsernameFromContextAsync: Kerberos enriched resolved username: {username}");
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Kerberos enriched username resolution failed, falling back to JWT.");
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
                    allowJwtResolution = unitOfWork.ClusterProjectRepository.AsQueryable()
                        .Where(x => x.ProjectId == project.Id && !x.IsDeleted)
                        .Any(x => x.PreferredAuthType == ClusterAuthenticationCredentialsAuthType.Kerberos);
                }
            }

            if (allowJwtResolution)
            {
                logger.LogWarning("ResolveUsernameFromContextAsync: Attempting JWT preferred_username resolution.");
                var token = !string.IsNullOrEmpty(httpContextKeys.Context.IdpToken) ? httpContextKeys.Context.IdpToken : httpContextKeys.Context.LEXISToken;
                if (!string.IsNullOrEmpty(token))
                {
                    try 
                    {
                        var decoded = JwtTokenDecoder.Decode(token);
                        if (!string.IsNullOrEmpty(decoded.PreferedUsername)) {
                            username = decoded.PreferedUsername;
                            logger.LogWarning($"ResolveUsernameFromContextAsync: JWT resolved username: {username}");
                        } else if (project != null) {
                            username = StringUtils.GenerateUsername(adaptorUserId ?? 0, project.AccountingString);
                            logger.LogWarning($"ResolveUsernameFromContextAsync: StringUtils.GenerateUsername resolved username: {username}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to decode JWT token for username resolution.");
                    }
                }
            }
        }

        logger.LogWarning($"ResolveUsernameFromContextAsync: End username resolution. Resolved username: {username}");
        return username;
    }
}
