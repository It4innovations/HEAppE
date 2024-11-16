using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.ClusterInformation;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.Utils;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;

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

        public IEnumerable<ClusterExt> ListAvailableClusters(string clusterName, string nodeTypeName, string projectName, string commandTemplateName)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                ClusterExt[] value;
                string memoryCacheKey = nameof(ListAvailableClusters);

                if (_cacheProvider.TryGetValue(memoryCacheKey, out value))
                {
                    _log.Info($"Using Memory Cache to get value for key: \"{memoryCacheKey}\"");
                }
                else
                {
                    _log.Info($"Reloading Memory Cache value for key: \"{memoryCacheKey}\"");
                    IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                    value = clusterLogic.ListAvailableClusters().Select(s => s.ConvertIntToExt()).ToArray();
                    _cacheProvider.Set(memoryCacheKey, value, TimeSpan.FromMinutes(_cacheLimitForListAvailableClusters));
                }

                if (clusterName is null && nodeTypeName is null && projectName is null && commandTemplateName is null)
                    return value;

                var clusters = JsonConvert.DeserializeObject<ClusterExt[]>(JsonConvert.SerializeObject(value));

                clusters = clusters.Where(x => clusterName is null || x.Name == clusterName).ToArray<ClusterExt>();
                foreach (var cl in clusters)
                {
                    if (nodeTypeName is not null)
                        cl.NodeTypes = cl.NodeTypes.Where(x => x.Name == nodeTypeName).ToArray<ClusterNodeTypeExt>();

                    foreach (var nt in cl.NodeTypes)
                    {
                        if (projectName is not null)
                            nt.Projects = nt.Projects.Where(x => x.Name == projectName).ToArray<ProjectExt>();

                        if (commandTemplateName is not null)
                        {
                            foreach (var pr in nt.Projects)
                                pr.CommandTemplates = pr.CommandTemplates.Where(x => x.Name == commandTemplateName).ToArray<CommandTemplateExt>();

                            nt.Projects = nt.Projects.Where(x => x.CommandTemplates.Length > 0).ToArray<ProjectExt>();
                        }
                    }

                    if (commandTemplateName is not null || projectName is not null)
                        cl.NodeTypes = cl.NodeTypes.Where(x => x.Projects.Length > 0).ToArray<ClusterNodeTypeExt>();
                }

                if (commandTemplateName is not null || projectName is not null || nodeTypeName is not null)
                    clusters = clusters.Where(x => x.NodeTypes.Length > 0).ToArray<ClusterExt>();

                return clusters;
            }
        }

        public IEnumerable<string> RequestCommandTemplateParametersName(long commandTemplateId, long projectId, string userScriptPath, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, projectId);

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

        public ClusterNodeUsageExt GetCurrentClusterNodeUsage(long clusterNodeId, long projectId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Reporter, projectId);

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
    }
}