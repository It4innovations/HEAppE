using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using Microsoft.Extensions.Caching.Memory;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExternalAuthentication.DTO.LexisAuth;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.Services.UserOrg;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.Utils;
using Microsoft.Extensions.Primitives;
using SshCaAPI;
using HEAppE.Services.Expirio;
using Microsoft.Extensions.Logging;

namespace HEAppE.ServiceTier.ClusterInformation;

public class ClusterInformationService : IClusterInformationService
{
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IUserOrgService _userOrgService;
    private readonly IExpirioService _expirioService;

    /// <summary>Active database refresh tasks for raw clusters list, used to coalesce concurrent DB queries globally.</summary>
    private static readonly ConcurrentDictionary<string, Task<List<HEAppE.DomainObjects.ClusterInformation.Cluster>>> _globalDbRefreshes = new();

    public ClusterInformationService(IMemoryCache cacheProvider, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys, IExpirioService expirioService, ILogger logger)
    {
        _userOrgService = userOrgService;
        _expirioService = expirioService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService ?? throw new ArgumentNullException(nameof(sshCertificateAuthorityService));
        _httpContextKeys = httpContextKeys ?? throw new ArgumentNullException(nameof(httpContextKeys));
        _cacheProvider = cacheProvider;
        _logger = logger;
    }
    
    private void SetCacheWithGlobalToken<T>(string key, T value, int expirationMinutes)
    {
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(expirationMinutes));
    
        CacheUtils.AddClusterInvalidation(options);

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
        long userId;
        List<HEAppE.DomainObjects.JobManagement.Project> projects;
        string memoryCacheKey;

        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var roles = new List<AdaptorUserRoleType> { AdaptorUserRoleType.Reporter, AdaptorUserRoleType.ManagementAdmin, AdaptorUserRoleType.Manager };
            var (loggedUser, userProjects) = UserAndLimitationManagementService
                .GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _logger, roles, _expirioService);

            userId = loggedUser.Id;
            projects = userProjects.ToList();
            memoryCacheKey = $"{nameof(ListAvailableClusters)}_{userId}_{clusterName}_{nodeTypeName}_{projectName}_{(accountingString != null ? string.Join(",", accountingString) : "")}_{commandTemplateName}";

            // Fast cache path (no forceRefresh)
            if (!forceRefresh && _cacheProvider.TryGetValue(memoryCacheKey, out ClusterExt[] cachedClusters))
            {
                return cachedClusters;
            }
        } // DB connection released

        // Retrieve raw clusters list globally (coalesced database queries)
        string globalCacheKey = $"GlobalRawClusters_{clusterName ?? "All"}";
        List<HEAppE.DomainObjects.ClusterInformation.Cluster> clusters;

        if (forceRefresh)
        {
            var dbTask = _globalDbRefreshes.GetOrAdd(globalCacheKey, key =>
                Task.Run(() =>
                {
                    try
                    {
                        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
                        {
                            return unitOfWork.ClusterRepository.AsQueryable()
                                .Where(c => clusterName == null || c.Name == clusterName)
                                .ToList();
                        }
                    }
                    finally
                    {
                        _globalDbRefreshes.TryRemove(key, out _);
                    }
                })
            );

            clusters = await dbTask;
            SetCacheWithGlobalToken(globalCacheKey, clusters, _cacheLimitForListAvailableClusters);
        }
        else
        {
            if (!_cacheProvider.TryGetValue(globalCacheKey, out clusters))
            {
                var dbTask = _globalDbRefreshes.GetOrAdd(globalCacheKey, key =>
                    Task.Run(() =>
                    {
                        try
                        {
                            using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
                            {
                                return unitOfWork.ClusterRepository.AsQueryable()
                                    .Where(c => clusterName == null || c.Name == clusterName)
                                    .ToList();
                            }
                        }
                        finally
                        {
                            _globalDbRefreshes.TryRemove(key, out _);
                        }
                    })
                );

                clusters = await dbTask;
                SetCacheWithGlobalToken(globalCacheKey, clusters, _cacheLimitForListAvailableClusters);
            }
        }

        // Get Lexis permission rules if enabled
        CommandTemplatePermissionsModel lexisPermissions = null;
        if (LexisAuthenticationConfiguration.CheckCommandTemplatePermissions && !string.IsNullOrEmpty(_httpContextKeys.Context.LEXISToken))
        {
            string instanceId = HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath;
            lexisPermissions = await _userOrgService.GetCommandTemplatePermissionsAsync(
                _httpContextKeys.Context.LEXISToken,
                HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath,
                instanceId, _logger);
        }

        HashSet<string> accountingSet = accountingString != null ? new(accountingString) : null;

        // Build a lookup of cluster scheduler types to identify QScheduler clusters.
        // QScheduler clusters do not use command templates — jobs are submitted via dedicated
        // QScheduler endpoints — so the CommandTemplates filter must be bypassed for them.
        // However, the project-level role check still applies (project must exist in the user's list).
        var clusterSchedulerTypes = clusters.ToDictionary(c => c.Id, c => c.SchedulerType);

        var clustersExt = clusters
            .Select(c =>
            {
                var ext = c.ConvertIntToExt(projects, true);
                ext.UseCallback = HEAppE.HpcConnectionFramework.Configuration.ClusterRuntimeConfiguration.For(c.CustomConfiguration).EnableCallback;
                return ext;
            })
            .ToArray();

        clustersExt = clustersExt
            .Select(cl =>
            {
                bool isQScheduler = cl.Id.HasValue &&
                                    clusterSchedulerTypes.TryGetValue(cl.Id.Value, out var st) &&
                                    st == SchedulerType.QScheduler;

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
                            // QScheduler clusters don't require command templates — skip template count check,
                            // but the project must still be present (user has a role in it).
                            .Where(p => isQScheduler || p.CommandTemplates.Length > 0)
                            .ToArray();
                        return nt;
                    })
                    .Where(nt => nt.Projects.Length > 0)
                    .ToArray();
                return cl;
            })
            .Where(cl => cl.NodeTypes.Length > 0)
            .ToArray();

        // Update the long-lived cache so non-forceRefresh requests get fresh data
        SetCacheWithGlobalToken(memoryCacheKey, clustersExt, _cacheLimitForListAvailableClusters);

        return clustersExt;
    }
    

    public ClusterClearCacheInfoExt ListAvailableClustersClearCache(string sessionCode)
    {
        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger);

        // Validate user and projects
        var (loggedUser, projectIds) = UserAndLimitationManagementService
            .GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _logger, AdaptorUserRoleType.Administrator, _expirioService);

        if (loggedUser is null || !projectIds.Any())
            throw new Exception("Operation permission denied.");

        var clearedKeysCount = 0;
        if (_cacheProvider is MemoryCache memCache)
        {
            clearedKeysCount = memCache.Count;
            memCache.Clear();
        }

        CacheUtils.InvalidateAllCache(_logger);
        
        return new ClusterClearCacheInfoExt
        {
            ClearedKeysCount = clearedKeysCount,
            Timestamp = new SqlDateTime(DateTime.UtcNow).Value,
            Description = "Cache cleared for current user"
        };
    }


    public async Task<IEnumerable<string>> RequestCommandTemplateParametersName(long commandTemplateId, long projectId,
        string userScriptPath, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, projectId, _expirioService);

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
                _logger.LogInformation($"Using Memory Cache to get value for key.");
                return value;
            }

            _logger.LogInformation($"Reloading Memory Cache value for key.");
            var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var result =
                await clusterLogic.GetCommandTemplateParametersName(commandTemplateId, projectId, userScriptPath, loggedUser);
            SetCacheWithGlobalToken(memoryCacheKey, result, _cacheLimitForGetCommandTemplateParametersName);
            return result;
        }
    }

    public async Task<ClusterNodeUsageExt> GetCurrentClusterNodeUsage(long clusterNodeId, long projectId,
        string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Reporter, projectId, _expirioService);

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
                _logger.LogInformation($"Using Memory Cache to get value for key.");
                return value;
            }

            _logger.LogInformation($"Reloading Memory Cache value for key.");
            var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork,  _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var nodeUsage = await clusterLogic.GetCurrentClusterNodeUsageAsync(clusterNodeId, loggedUser, projectId);
            SetCacheWithGlobalToken(memoryCacheKey, nodeUsage.ConvertIntToExt(), _cacheLimitForGetCurrentClusterUsage);
            return nodeUsage.ConvertIntToExt();
        }
    }

    public async Task<string> GetMachineArchitecture(long clusterNodeTypeId, long projectId, string sessionCode)
    {
        _logger.LogInformation($"GetMachineArchitecture service tier call. clusterNodeTypeId: {clusterNodeTypeId}, projectId: {projectId}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Reporter, projectId, _expirioService);

            var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var result = await clusterLogic.GetMachineArchitectureAsync(clusterNodeTypeId, loggedUser, projectId);
            _logger.LogInformation($"GetMachineArchitecture service tier returning result for clusterNodeTypeId: {clusterNodeTypeId}");
            return result;
        }
    }

    public async Task<string> GetMachineCalibration(long clusterNodeTypeId, string calibrationId, string endpoint, long projectId, string sessionCode)
    {
        _logger.LogInformation($"GetMachineCalibration service tier call. clusterNodeTypeId: {clusterNodeTypeId}, calibrationId: '{calibrationId}', endpoint: '{endpoint}', projectId: {projectId}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Reporter, projectId, _expirioService);

            var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
            var result = await clusterLogic.GetMachineCalibrationAsync(clusterNodeTypeId, calibrationId, endpoint, loggedUser, projectId);
            _logger.LogInformation($"GetMachineCalibration service tier returning result for clusterNodeTypeId: {clusterNodeTypeId}");
            return result;
        }
    }

    #region Instances

    /// <summary>
    ///     Logger
    /// </summary>
    private readonly ILogger _logger;

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