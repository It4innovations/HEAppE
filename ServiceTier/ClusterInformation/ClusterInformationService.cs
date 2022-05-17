using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.ClusterInformation;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using log4net;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.ServiceTier.UserAndLimitationManagement.Roles;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace HEAppE.ServiceTier.ClusterInformation
{
    public class ClusterInformationService : IClusterInformationService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IMemoryCache _cacheProvider;

        public ClusterInformationService(IMemoryCache cacheProvider)
        {
            _cacheProvider = cacheProvider;
        }

        public ClusterExt[] ListAvailableClusters()
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                    IList<Cluster> clusters = clusterLogic.ListAvailableClusters();
                    return clusters.Select(s => s.ConvertIntToExt()).ToArray();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public ClusterNodeUsageExt GetCurrentClusterNodeUsage(long clusterNodeId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter);

                    string memoryCacheKey = StringUtils.CreateIdentifierHash(
                    new List<string>()
                        {    clusterNodeId.ToString(),
                             nameof(GetCurrentClusterNodeUsage)
                        }
                    );

                    if (_cacheProvider.TryGetValue(memoryCacheKey, out ClusterNodeUsageExt value))
                    {
                        log.Info($"Using Memory Cache to get value for key: \"{memoryCacheKey}\"");
                        return value;
                    }
                    else
                    {
                        log.Info($"Reloading Memory Cache value for key: \"{memoryCacheKey}\"");
                        IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                        ClusterNodeUsage nodeUsage = clusterLogic.GetCurrentClusterNodeUsage(clusterNodeId, loggedUser);
                        _cacheProvider.Set(memoryCacheKey, nodeUsage.ConvertIntToExt(), TimeSpan.FromMinutes(2));
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
        public IEnumerable<string> GetCommandTemplateParametersName(long commandTemplateId, string userScriptPath, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter);
                    
                    string memoryCacheKey = StringUtils.CreateIdentifierHash(
                    new List<string>()
                        {   commandTemplateId.ToString(),
                            userScriptPath,
                            nameof(GetCommandTemplateParametersName)
                        }
                    );

                    if (_cacheProvider.TryGetValue(memoryCacheKey, out IEnumerable<string> value))
                    {
                        log.Info($"Using Memory Cache to get value for key: \"{memoryCacheKey}\"");
                        return value;
                    }
                    else
                    {
                        log.Info($"Reloading Memory Cache value for key: \"{memoryCacheKey}\"");
                        IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                        IEnumerable<string> result = clusterLogic.GetCommandTemplateParametersName(commandTemplateId, userScriptPath, loggedUser);
                        _cacheProvider.Set(memoryCacheKey, result, TimeSpan.FromMinutes(2));
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

    }
}