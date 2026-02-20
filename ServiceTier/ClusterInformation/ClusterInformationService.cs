using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExternalAuthentication.DTO.LexisAuth;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.Services.UserOrg;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.Utils;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using SshCaAPI;

namespace HEAppE.ServiceTier.ClusterInformation;

public class ClusterInformationService : IClusterInformationService
{
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IUserOrgService _userOrgService;
    public ClusterInformationService(IMemoryCache cacheProvider, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        _userOrgService = userOrgService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService ?? throw new ArgumentNullException(nameof(sshCertificateAuthorityService));
        _httpContextKeys = httpContextKeys ?? throw new ArgumentNullException(nameof(httpContextKeys));
        _cacheProvider = cacheProvider;
        _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }
    
    private void SetCacheWithGlobalToken<T>(string key, T value, int expirationMinutes)
    {
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(expirationMinutes))
            .AddExpirationToken(new CancellationChangeToken(CacheUtils.GlobalResetToken));
    
        _cacheProvider.Set(key, value, options);
    }

    public async Task<IEnumerable<ClusterExt>> ListAvailableClusters(string sessionCode,
        string clusterName,
        string nodeTypeName,
        string projectName,
        string[] accountingString,
        string commandTemplateName,
        bool forceRefresh)
    {
        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();

        var roles = new List<AdaptorUserRoleType> { AdaptorUserRoleType.Reporter, AdaptorUserRoleType.ManagementAdmin, AdaptorUserRoleType.Manager };

        var (loggedUser, projects) = UserAndLimitationManagementService
            .GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, roles);

        var memoryCacheKey = $"{nameof(ListAvailableClusters)}_{loggedUser.Id}_{clusterName}_{nodeTypeName}_{projectName}_{(accountingString != null ? string.Join(",", accountingString) : "")}_{commandTemplateName}";

        if (!forceRefresh && _cacheProvider.TryGetValue(memoryCacheKey, out ClusterExt[] cachedClusters))
        {
            return cachedClusters;
        }

        CommandTemplatePermissionsModel lexisPermissions = null;
        if (LexisAuthenticationConfiguration.CheckCommandTemplatePermissions && !string.IsNullOrEmpty(_httpContextKeys.Context.LEXISToken))
        {
            lexisPermissions = await _userOrgService.GetCommandTemplatePermissionsAsync(
                _httpContextKeys.Context.LEXISToken,
                HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath);
        }

        HashSet<string> accountingSet = accountingString != null ? new(accountingString) : null;

        var clusters = unitOfWork.ClusterRepository.AsQueryable()
            .Where(c => clusterName == null || c.Name == clusterName)
            .ToList();

        var clustersExt = clusters
            .Select(c => c.ConvertIntToExt(projects, true))
            .ToArray();

        clustersExt = clustersExt
            .Select(cl =>
            {
                cl.NodeTypes = cl.NodeTypes
                    .Where(nt => nodeTypeName == null || nt.Name == nodeTypeName)
                    .Select(nt =>
                    {
                        nt.Projects = nt.Projects
                            .Where(p => (projectName == null || p.Name == projectName) && (accountingSet == null || accountingSet.Contains(p.AccountingString)))
                            .Select(p =>
                            {
                                p.CommandTemplates = p.CommandTemplates
                                    .Where(ct => (commandTemplateName == null || string.Equals(ct.Name, commandTemplateName, StringComparison.OrdinalIgnoreCase)) &&
                                                 (lexisPermissions == null || _userOrgService.IsTemplateEnabledInLexis(lexisPermissions, cl.Name, nt.Name, p.AccountingString, ct.Name)))
                                    .ToArray();
                                return p;
                            })
                            .Where(p => p.CommandTemplates.Length > 0)
                            .ToArray();
                        return nt;
                    })
                    .Where(nt => nt.Projects.Length > 0)
                    .ToArray();
                return cl;
            })
            .Where(cl => cl.NodeTypes.Length > 0)
            .ToArray();
        
        SetCacheWithGlobalToken(memoryCacheKey, clustersExt, _cacheLimitForListAvailableClusters);

        return clustersExt;
    }
    

    public ClusterClearCacheInfoExt ListAvailableClustersClearCache(string sessionCode)
    {
        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();

        // Validate user and projects
        var (loggedUser, projectIds) = UserAndLimitationManagementService
            .GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, AdaptorUserRoleType.Administrator);

        if (loggedUser is null || !projectIds.Any())
            throw new Exception("Operation permission denied.");

        var clearedKeysCount = 0;
        if (_cacheProvider is MemoryCache memCache)
        {
            clearedKeysCount = memCache.Count;
        }

        CacheUtils.InvalidateAllCache();
        
        return new ClusterClearCacheInfoExt
        {
            ClearedKeysCount = clearedKeysCount,
            Timestamp = new SqlDateTime(DateTime.UtcNow).Value,
            Description = "Cache cleared for current user using global reset token"
        };
    }


    public async Task<IEnumerable<string>> RequestCommandTemplateParametersName(long commandTemplateId, long projectId,
        string userScriptPath, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                AdaptorUserRoleType.Submitter, projectId);

            var memoryCacheKey = StringUtils.CreateIdentifierHash(
                new List<string>
                {
                    commandTemplateId.ToString(),
                    projectId.ToString(),
                    userScriptPath,
                    nameof(RequestCommandTemplateParametersName)
                }
            );

            if (_cacheProvider.TryGetValue(memoryCacheKey, out IEnumerable<string> value))
            {
                _log.Info($"Using Memory Cache to get value for key.");
                return value;
            }

            _log.Info($"Reloading Memory Cache value for key.");
            var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
            var result =
                await clusterLogic.GetCommandTemplateParametersName(commandTemplateId, projectId, userScriptPath, loggedUser);
            SetCacheWithGlobalToken(memoryCacheKey, result, _cacheLimitForGetCommandTemplateParametersName);
            return result;
        }
    }

    public async Task<ClusterNodeUsageExt> GetCurrentClusterNodeUsage(long clusterNodeId, long projectId,
        string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                AdaptorUserRoleType.Reporter, projectId);

            //Memory cache key with personal session code due security purpose of access to cluster reference to project
            var memoryCacheKey = StringUtils.CreateIdentifierHash(
                new List<string>
                {
                    clusterNodeId.ToString(),
                    sessionCode,
                    nameof(GetCurrentClusterNodeUsage)
                }
            );

            if (_cacheProvider.TryGetValue(memoryCacheKey, out ClusterNodeUsageExt value))
            {
                _log.Info($"Using Memory Cache to get value for key.");
                return value;
            }

            _log.Info($"Reloading Memory Cache value for key.");
            var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork,  _sshCertificateAuthorityService, _httpContextKeys);
            var nodeUsage = await clusterLogic.GetCurrentClusterNodeUsage(clusterNodeId, loggedUser, projectId);
            SetCacheWithGlobalToken(memoryCacheKey, nodeUsage.ConvertIntToExt(), _cacheLimitForGetCurrentClusterUsage);
            return nodeUsage.ConvertIntToExt();
        }
    }

    #region Instances

    /// <summary>
    ///     Logger
    /// </summary>
    private readonly ILog _log;

    /// <summary>
    ///     Cache provider
    /// </summary>
    private readonly IMemoryCache _cacheProvider;

    /// <summary>
    ///     Cache limit in minutes for method ListAvailableClusters
    /// </summary>
    private readonly int _cacheLimitForListAvailableClusters = 150;

    /// <summary>
    ///     Cache limit in minutes for method GetCommandTemplateParametersName
    /// </summary>
    private readonly int _cacheLimitForGetCommandTemplateParametersName = 2;

    /// <summary>
    ///     Cache limit in minutes for method GetCurrentClusterUsage
    /// </summary>
    private readonly int _cacheLimitForGetCurrentClusterUsage = 2;

    #endregion
}