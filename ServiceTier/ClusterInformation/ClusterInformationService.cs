using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.Utils;
using log4net;

namespace HEAppE.ServiceTier.ClusterInformation;

public class ClusterInformationService : IClusterInformationService
{
    public ClusterInformationService(IMemoryCache cacheProvider)
    {
        _cacheProvider = cacheProvider;
        _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }

    public IEnumerable<ClusterExt> ListAvailableClusters(string sessionCode, string clusterName, string nodeTypeName,
    string projectName, string[] accountingString, string commandTemplateName, bool forceRefresh)
    {
        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();

        var roles = new List<AdaptorUserRoleType>
        {
            AdaptorUserRoleType.Reporter,
            AdaptorUserRoleType.ManagementAdmin,
            AdaptorUserRoleType.Manager
        };

        var (loggedUser, projects) = UserAndLimitationManagementService
            .GetValidatedUserForSessionCode(sessionCode, unitOfWork, roles);

        var memoryCacheKey = $"{nameof(ListAvailableClusters)}_{sessionCode}";
        if (!forceRefresh && _cacheProvider.TryGetValue(memoryCacheKey, out ClusterExt[] cachedClusters))
        {
            _log.Info($"Using Memory Cache to get value for key: \"{memoryCacheKey}\"");
        }
        else
        {
            _log.Info($"Reloading Memory Cache value for key: \"{memoryCacheKey}\"");
            var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
            cachedClusters = clusterLogic.ListAvailableClusters()
                .Select(c => c.ConvertIntToExt(projects, true))
                .ToArray();
            _cacheProvider.Set(memoryCacheKey, cachedClusters, TimeSpan.FromMinutes(_cacheLimitForListAvailableClusters));
        }

        // Pokud nejsou žádné filtry, vrať cache rovnou
        if (clusterName == null && nodeTypeName == null && projectName == null && accountingString == null && commandTemplateName == null)
            return cachedClusters;

        HashSet<string> accountingSet = accountingString != null ? new(accountingString) : null;

        var filteredClusters = cachedClusters
            .Where(c => clusterName == null || c.Name == clusterName)
            .Select(cl =>
            {
                cl.NodeTypes = cl.NodeTypes
                    .Where(nt => nodeTypeName == null || nt.Name == nodeTypeName)
                    .Select(nt =>
                    {
                        nt.Projects = nt.Projects
                            .Where(p =>
                                (projectName == null || p.Name == projectName) &&
                                (accountingSet == null || accountingSet.Contains(p.AccountingString)) &&
                                (commandTemplateName == null || p.CommandTemplates.Any(ct => ct.Name == commandTemplateName))
                            )
                            .Select(p =>
                            {
                                if (commandTemplateName != null)
                                    p.CommandTemplates = p.CommandTemplates
                                        .Where(ct => ct.Name == commandTemplateName)
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

        return filteredClusters;
    }


    public ClusterClearCacheInfoExt ListAvailableClustersClearCache(string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var (loggedUser, projectIds) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Manager);
            if (loggedUser is null || !projectIds.Any())
                throw new Exception("Operation permission denied.");
        }

        var clearedKeysCount = 0;
        var collection = (_cacheProvider as MemoryCache).Keys;
        foreach (var item in collection ?? [])
        {
            var memoryCacheKey = item.ToString();
            if (memoryCacheKey.StartsWith($"{nameof(ListAvailableClusters)}_"))
            {
                _cacheProvider.Remove(memoryCacheKey);
                ++clearedKeysCount;
            }
        }

        return new ClusterClearCacheInfoExt
        {
            ClearedKeysCount = clearedKeysCount,
            Timestamp = new SqlDateTime(DateTime.UtcNow).Value,
            Description = "Cache cleared",
        };
    }

    public IEnumerable<string> RequestCommandTemplateParametersName(long commandTemplateId, long projectId,
        string userScriptPath, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
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
                _log.Info($"Using Memory Cache to get value for key: \"{memoryCacheKey}\"");
                return value;
            }

            _log.Info($"Reloading Memory Cache value for key: \"{memoryCacheKey}\"");
            var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
            var result =
                clusterLogic.GetCommandTemplateParametersName(commandTemplateId, projectId, userScriptPath, loggedUser);
            _cacheProvider.Set(memoryCacheKey, result,
                TimeSpan.FromMinutes(_cacheLimitForGetCommandTemplateParametersName));
            return result;
        }
    }

    public ClusterNodeUsageExt GetCurrentClusterNodeUsage(long clusterNodeId, long projectId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
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
                _log.Info($"Using Memory Cache to get value for key: \"{memoryCacheKey}\"");
                return value;
            }

            _log.Info($"Reloading Memory Cache value for key: \"{memoryCacheKey}\"");
            var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
            var nodeUsage = clusterLogic.GetCurrentClusterNodeUsage(clusterNodeId, loggedUser, projectId);
            _cacheProvider.Set(memoryCacheKey, nodeUsage.ConvertIntToExt(),
                TimeSpan.FromMinutes(_cacheLimitForGetCurrentClusterUsage));
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