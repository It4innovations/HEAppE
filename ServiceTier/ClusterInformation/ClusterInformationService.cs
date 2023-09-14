using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.BusinessLogicTier.Logic.ClusterInformation;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.Utils;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HEAppE.ServiceTier.ClusterInformation
{
    public class ClusterInformationService : IClusterInformationService
    {
        #region Instances
        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILog _log;

        /// <summary>
        /// Cache provider
        /// </summary>
        private readonly IMemoryCache _cacheProvider;

        /// <summary>
        /// Cache limit in minutes for method ListAvailableClusters
        /// </summary>
        private readonly int _cacheLimitForListAvailableClusters = 150;

        /// <summary>
        /// Cache limit in minutes for method GetCommandTemplateParametersName 
        /// </summary>
        private readonly int _cacheLimitForGetCommandTemplateParametersName = 2;

        /// <summary>
        /// Cache limit in minutes for method GetCurrentClusterUsage 
        /// </summary>
        private readonly int _cacheLimitForGetCurrentClusterUsage = 2;
        #endregion
        public ClusterInformationService(IMemoryCache cacheProvider)
        {
            _cacheProvider = cacheProvider;
            _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public IEnumerable<ClusterExt> ListAvailableClusters()
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    string memoryCacheKey = nameof(ListAvailableClusters);

                    if (_cacheProvider.TryGetValue(memoryCacheKey, out ClusterExt[] value))
                    {
                        _log.Info($"Using Memory Cache to get value for key: \"{memoryCacheKey}\"");
                        return value;
                    }
                    else
                    {
                        _log.Info($"Reloading Memory Cache value for key: \"{memoryCacheKey}\"");
                        IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                        var clusters = clusterLogic.ListAvailableClusters();
                        var result = clusters.Select(s => s.ConvertIntToExt()).ToArray();
                        _cacheProvider.Set(memoryCacheKey, result, TimeSpan.FromMinutes(_cacheLimitForListAvailableClusters));
                        return result;
                    }
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public IEnumerable<string> RequestCommandTemplateParametersName(long commandTemplateId, long projectId, string userScriptPath, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, projectId);

                    string memoryCacheKey = StringUtils.CreateIdentifierHash(
                    new List<string>()
                        {   commandTemplateId.ToString(),
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
                    else
                    {
                        _log.Info($"Reloading Memory Cache value for key: \"{memoryCacheKey}\"");
                        IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                        IEnumerable<string> result = clusterLogic.GetCommandTemplateParametersName(commandTemplateId, projectId, userScriptPath, loggedUser);
                        _cacheProvider.Set(memoryCacheKey, result, TimeSpan.FromMinutes(_cacheLimitForGetCommandTemplateParametersName));
                        return result;
                    }
                }
            }
            catch (Exception exc)
            {
                //TODO Should be rewrite!
                if (exc.Message.Contains("No such file or directory") || exc.Message.Contains("Is a directory"))
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(exc.Message));
                }

                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public ClusterNodeUsageExt GetCurrentClusterNodeUsage(long clusterNodeId, long projectId,
            string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter, projectId);

                    //Memory cache key with personal session code due security purpose of access to cluster reference to project
                    string memoryCacheKey = StringUtils.CreateIdentifierHash(
                    new List<string>()
                        {    clusterNodeId.ToString(),
                             sessionCode,
                             nameof(GetCurrentClusterNodeUsage)
                        }
                    );

                    if (_cacheProvider.TryGetValue(memoryCacheKey, out ClusterNodeUsageExt value))
                    {
                        _log.Info($"Using Memory Cache to get value for key: \"{memoryCacheKey}\"");
                        return value;
                    }
                    else
                    {
                        _log.Info($"Reloading Memory Cache value for key: \"{memoryCacheKey}\"");
                        IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                        ClusterNodeUsage nodeUsage = clusterLogic.GetCurrentClusterNodeUsage(clusterNodeId, loggedUser, projectId);
                        _cacheProvider.Set(memoryCacheKey, nodeUsage.ConvertIntToExt(), TimeSpan.FromMinutes(_cacheLimitForGetCurrentClusterUsage));
                        return nodeUsage.ConvertIntToExt();
                    }
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }
    }
}