using HEAppE.BusinessLogicTier.Logic;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.ExtModels.ClusterInformation.Models;
using System;
using System.Linq;

namespace HEAppE.ExtModels.ClusterInformation.Converts
{
    public static class ClusterInformationConverts
    {
        #region Public Methods
        public static ClusterExt ConvertIntToExt(this Cluster cluster)
        {
            var convert = new ClusterExt()
            {
                Id = cluster.Id,
                Name = cluster.Name,
                Description = cluster.Description,
                NodeTypes = cluster.NodeTypes.Select(s => s.ConvertIntToExt())
                                              .ToArray()
            };
            return convert;
        }

        public static ClusterNodeTypeExt ConvertIntToExt(this ClusterNodeType nodeType)
        {
            var convert = new ClusterNodeTypeExt()
            {
                Id = nodeType.Id,
                Name = nodeType.Name,
                Description = nodeType.Description,
                NumberOfNodes = nodeType.NumberOfNodes,
                CoresPerNode = nodeType.CoresPerNode,
                MaxWalltime = nodeType.MaxWalltime,
                FileTransferMethodId = nodeType.FileTransferMethodId,
                CommandTemplates = nodeType.PossibleCommands.Where(c=>c.IsEnabled).Select(s=> s.ConvertIntToExt())
                                                             .ToArray()
            };
            return convert;
        }

        public static CommandTemplateExt ConvertIntToExt(this CommandTemplate commandTemplate)
        {
            var convert = new CommandTemplateExt()
            {
                Id = commandTemplate.Id,
                Name = commandTemplate.Name,
                Description = commandTemplate.Description,
                Code = commandTemplate.Code,
                IsGeneric = commandTemplate.IsGeneric,
                TemplateParameters = commandTemplate.TemplateParameters.Where(w=> string.IsNullOrEmpty(w.Query) && w.IsVisible)
                                                                        .Select(s=>s.ConvertIntToExt())
                                                                        .ToArray()
            };
            return convert;
        }

        private static CommandTemplateParameterExt ConvertIntToExt(this CommandTemplateParameter templateParameter)
        {
            var convert = new CommandTemplateParameterExt
            {
                Identifier = templateParameter.Identifier,
                Description = templateParameter.Description
            };
            return convert;
        }

        public static ClusterNodeUsageExt ConvertIntToExt(this ClusterNodeUsage nodeUsage)
        {
            var convert = new ClusterNodeUsageExt
            {
                Id = nodeUsage.NodeType.Id,
                Name = nodeUsage.NodeType.Name,
                Description = nodeUsage.NodeType.Description,
                Priority = nodeUsage.Priority,
                CoresPerNode = nodeUsage.NodeType.CoresPerNode,
                MaxWalltime = nodeUsage.NodeType.MaxWalltime,
                NumberOfNodes = nodeUsage.NodeType.NumberOfNodes,
                NumberOfUsedNodes = nodeUsage.NodesUsed,
                TotalJobs = nodeUsage.TotalJobs
            };

            return convert;
        }

        public static ClusterProxyConnectionExt ConvertIntToExt(this ClusterProxyConnection proxyConnection)
        {
            var convert = new ClusterProxyConnectionExt
            {
                Host = proxyConnection.Host,
                Port = proxyConnection.Port,
                Type = ConvertProxyTypeIntToExt(proxyConnection.Type),
                Username = proxyConnection.Username,
                Password = proxyConnection.Password
            };

            return convert;
        }
        #endregion
        #region Private Methods
        private static ProxyTypeExt ConvertProxyTypeIntToExt(ProxyType? proxyType)
        {
            if (!proxyType.HasValue)
            {
                throw new InputValidationException("The Proxy type must be set.");
            }

            if (!Enum.TryParse(proxyType.ToString(), out ProxyTypeExt convert))
            {
                throw new InputValidationException("The Proxy type must have value from <1, 2, 3, 4>.");
            }

            return convert;
        }
        #endregion
    }
}