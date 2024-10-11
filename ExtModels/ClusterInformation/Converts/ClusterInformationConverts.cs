using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobReporting.Converts;
using HEAppE.ExtModels.UserAndLimitationManagement.Converts;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System;
using System.Linq;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Models;
using System.Collections.Generic;

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
            // get all projects
            var projectExts = new List<ProjectExt>();
            if (nodeType.Cluster != null)
            {
                var projects = nodeType.Cluster.ClusterProjects?.Select(x => x.Project).ToList();
                projectExts = projects?.Select(x => x.ConvertIntToExt()).ToList() ?? new List<ProjectExt>();

                // select possible commands for specific project or command for all projects
                foreach (var project in projectExts)
                {
                    project.CommandTemplates = nodeType.PossibleCommands.Where(c => c.IsEnabled && (!c.ProjectId.HasValue || c.ProjectId == project.Id))
                                                                            .Select(command => command.ConvertIntToExt())
                                                                                .ToArray();

                }
            }

            var convert = new ClusterNodeTypeExt()
            {
                Id = nodeType.Id,
                Name = nodeType.Name,
                Description = nodeType.Description,
                NumberOfNodes = nodeType.NumberOfNodes,
                CoresPerNode = nodeType.CoresPerNode,
                MaxWalltime = nodeType.MaxWalltime,
                FileTransferMethodId = nodeType.FileTransferMethodId,
                Projects = projectExts.Where(p => p.CommandTemplates.Any()).ToArray()
            };
            return convert;
        }

        public static ClusterNodeTypeResourceUsageExt ConvertIntToExt(this ClusterNodeTypeResourceUsage nodeType)
        {
            var convert = new ClusterNodeTypeResourceUsageExt()
            {
                Id = nodeType.Id,
                Name = nodeType.Name,
                Description = nodeType.Description,
                NumberOfNodes = nodeType.NumberOfNodes,
                CoresPerNode = nodeType.CoresPerNode,
                MaxWalltime = nodeType.MaxWalltime,
                FileTransferMethodId =  nodeType.FileTransferMethod?.Id,
                NodeUsedCoresAndLimitation = nodeType.NodeUsedCoresAndLimitation.ConvertIntToExt(),
            };
            return convert;
        }

        public static ClusterNodeTypeAggregationExt ConvertIntToExt(this ClusterNodeTypeAggregation aggregation)
        {
            var convert = new ClusterNodeTypeAggregationExt()
            {
                Id = aggregation.Id,
                Name = aggregation.Name,
                Description = aggregation.Description,
                AllocationType = aggregation.AllocationType,
                ValidityFrom = aggregation.ValidityFrom,
                ValidityTo = aggregation.ValidityTo
            };
            return convert;
        }

        public static ClusterNodeTypeAggregationAccountingExt ConvertIntToExt(this ClusterNodeTypeAggregationAccounting accounting)
        {
            var convert = new ClusterNodeTypeAggregationAccountingExt()
            {
                ClusterNodeTypeAggregationId = accounting.ClusterNodeTypeAggregationId,
                AccountingId = accounting.AccountingId
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
                ExtendedAllocationCommand = commandTemplate.ExtendedAllocationCommand,
                IsGeneric = commandTemplate.IsGeneric,
                TemplateParameters = commandTemplate.TemplateParameters.Where(w => string.IsNullOrEmpty(w.Query) && w.IsVisible)
                                                                        .Select(s => s.ConvertIntToExt())
                                                                        .ToArray()
            };
            return convert;
        }
        
        public static ExtendedCommandTemplateExt ConvertIntToExtendedExt(this CommandTemplate commandTemplate)
        {
            var convert = new ExtendedCommandTemplateExt()
            {
                Id = commandTemplate.Id,
                Name = commandTemplate.Name,
                Description = commandTemplate.Description,
                ExtendedAllocationCommand = commandTemplate.ExtendedAllocationCommand,
                IsGeneric = commandTemplate.IsGeneric,
                PreparationScript = commandTemplate.PreparationScript,
                ExecutableFile = commandTemplate.ExecutableFile,
                CommandParameters = commandTemplate.CommandParameters,
                ProjectId = commandTemplate.ProjectId.HasValue? commandTemplate.ProjectId.Value : 0,
                ClusterNodeTypeId = commandTemplate.ClusterNodeTypeId.HasValue? commandTemplate.ClusterNodeTypeId.Value : 0,
                TemplateParameters = commandTemplate.TemplateParameters.Where(w => string.IsNullOrEmpty(w.Query) && w.IsVisible)
                    .Select(s => s.ConvertIntToExtendedExt())
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
        
        public static ExtendedCommandTemplateParameterExt ConvertIntToExtendedExt(this CommandTemplateParameter templateParameter)
        {
            var convert = new ExtendedCommandTemplateParameterExt
            {
                Id = templateParameter.Id,
                Identifier = templateParameter.Identifier,
                Description = templateParameter.Description,
                Query = templateParameter.Query
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
                Id = proxyConnection.Id,
                Host = proxyConnection.Host,
                Port = proxyConnection.Port,
                Type = ConvertProxyTypeIntToExt(proxyConnection.Type),
                Username = proxyConnection.Username,
                Password = proxyConnection.Password
            };

            return convert;
        }
        
        public static SubProjectExt ConvertIntToExt(this SubProject subProject)
        {
            var convert = new SubProjectExt
            {
                Id = subProject.Id,
                Identifier = subProject.Identifier,
                Description = subProject.Description,
                StartDate = subProject.StartDate,
                EndDate = subProject.EndDate,
                ProjectId = subProject.ProjectId
            };

            return convert;
        }
        
        public static AccountingStateExt ConvertIntToExt(this AccountingState accountingState)
        {
            var convert = new AccountingStateExt
            {
                ProjectId = accountingState.ProjectId,
                State = accountingState.AccountingStateType.ConvertIntToExt(),
                ComputingStartDate = accountingState.ComputingStartDate,
                ComputingEndDate = accountingState.ComputingEndDate,
                TriggeredAt = accountingState.TriggeredAt,
                LastUpdatedAt = accountingState.LastUpdatedAt
            };

            return convert;
        }
        
        public static AccountingStateTypeExt ConvertIntToExt(this AccountingStateType accountingStateType)
        {
            return accountingStateType switch
            {
                AccountingStateType.Unknown => AccountingStateTypeExt.Unknown,
                AccountingStateType.Queued => AccountingStateTypeExt.Queued,
                AccountingStateType.Running => AccountingStateTypeExt.Running,
                AccountingStateType.Finished => AccountingStateTypeExt.Finished,
                AccountingStateType.Failed => AccountingStateTypeExt.Failed,
                _ => AccountingStateTypeExt.Unknown
            };
        }

        public static ClusterAuthenticationCredentialsAuthTypeExt ConvertIntToExt(
            this ClusterAuthenticationCredentialsAuthType type)
        {
            return type switch
            {
                ClusterAuthenticationCredentialsAuthType.Password => ClusterAuthenticationCredentialsAuthTypeExt
                    .Password,
                ClusterAuthenticationCredentialsAuthType.PasswordInteractive =>
                    ClusterAuthenticationCredentialsAuthTypeExt.PasswordInteractive,
                ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKey =>
                    ClusterAuthenticationCredentialsAuthTypeExt.PasswordAndPrivateKey,
                ClusterAuthenticationCredentialsAuthType.PrivateKey => ClusterAuthenticationCredentialsAuthTypeExt
                    .PrivateKey,
                ClusterAuthenticationCredentialsAuthType.PasswordViaProxy => ClusterAuthenticationCredentialsAuthTypeExt
                    .PasswordViaProxy,
                ClusterAuthenticationCredentialsAuthType.PasswordInteractiveViaProxy =>
                    ClusterAuthenticationCredentialsAuthTypeExt.PasswordInteractiveViaProxy,
                ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKeyViaProxy =>
                    ClusterAuthenticationCredentialsAuthTypeExt.PasswordAndPrivateKeyViaProxy,
                ClusterAuthenticationCredentialsAuthType.PrivateKeyViaProxy =>
                    ClusterAuthenticationCredentialsAuthTypeExt.PrivateKeyViaProxy,
                ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent =>
                    ClusterAuthenticationCredentialsAuthTypeExt.PrivateKeyInSshAgent,
                _ => ClusterAuthenticationCredentialsAuthTypeExt.Unknown
            };
        }
        
        #endregion
        #region Private Methods
        private static ProxyTypeExt ConvertProxyTypeIntToExt(ProxyType? proxyType)
        {
            if (!proxyType.HasValue)
            {
                throw new InputValidationException("EnumValueMustBeSet", "Proxy type");
            }

            if (!Enum.TryParse(proxyType.ToString(), out ProxyTypeExt convert))
            {
                throw new InputValidationException("EnumValueMustBeInInterval", "Proxy type", "<1, 2, 3, 4>");
            }

            return convert;
        }
        #endregion
    }
}