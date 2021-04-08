using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.ExtModels.ClusterInformation.Models;
using System;
using System.Linq;

namespace HEAppE.ExtModels.ClusterInformation.Converts
{
    public static class ClusterInformationConverts
    {
        public static ClusterExt ConvertIntToExt(this Cluster cluster)
        {
            ClusterExt convert = new ClusterExt()
            {
                Id = cluster.Id,
                Name = cluster.Name,
                Description = cluster.Description,
                NodeTypes = cluster.NodeTypes.Select(s=>s.ConvertIntToExt())
                                              .ToArray()
            };
            return convert;
        }

        public static ClusterNodeTypeExt ConvertIntToExt(this ClusterNodeType nodeType)
        {
            ClusterNodeTypeExt convert = new ClusterNodeTypeExt()
            {
                Id = nodeType.Id,
                Name = nodeType.Name,
                Description = nodeType.Description,
                NumberOfNodes = nodeType.NumberOfNodes,
                CoresPerNode = nodeType.CoresPerNode,
                MaxWalltime = nodeType.MaxWalltime,
                CommandTemplates = nodeType.PossibleCommands.Select(s=> s.ConvertIntToExt())
                                                             .ToArray()
            };
            return convert;
        }

        private static CommandTemplateExt ConvertIntToExt(this CommandTemplate commandTemplate)
        {
            CommandTemplateExt convert = new CommandTemplateExt()
            {
                Id = commandTemplate.Id,
                Name = commandTemplate.Name,
                Description = commandTemplate.Description,
                Code = commandTemplate.Code,
                TemplateParameters = commandTemplate.TemplateParameters.Where(w=> string.IsNullOrEmpty(w.Query))
                                                                        .Select(s=>s.ConvertIntToExt())
                                                                        .ToArray()
            };
            return convert;
        }

        private static CommandTemplateParameterExt ConvertIntToExt(this CommandTemplateParameter templateParameter)
        {
            CommandTemplateParameterExt convert = new CommandTemplateParameterExt
            {
                Identifier = templateParameter.Identifier,
                Description = templateParameter.Description
            };
            return convert;
        }

        public static ClusterNodeUsageExt ConvertIntToExt(this ClusterNodeUsage nodeUsage)
        {
            ClusterNodeUsageExt convert = new ClusterNodeUsageExt
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
    }
}