using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Converts;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.HpcConnectionFramework.Configuration;

namespace HEAppE.ExtModels.ClusterInformation.Converts;

public static class ClusterInformationConverts
{
    #region Private Methods
    private static ProxyTypeExt ConvertProxyTypeIntToExt(ProxyType? proxyType)
    {
        if (!proxyType.HasValue) throw new InputValidationException("EnumValueMustBeSet", "Proxy type");

        if (!Enum.TryParse(proxyType.ToString(), out ProxyTypeExt convert))
            throw new InputValidationException("EnumValueMustBeInInterval", "Proxy type", "<1, 2, 3, 4>");

        return convert;
    }

    #endregion

    #region Public Methods

    public static ClusterExt ConvertIntToExt(this Cluster cluster, IEnumerable<Project> projects, bool onlyActive)
    {
        var convert = new ClusterExt
        {
            Id = cluster.Id,
            Name = cluster.Name,
            Description = cluster.Description,
            MasterNodeName = cluster.MasterNodeName,
            FileTransferMethodIds = cluster.FileTransferMethods.Select(x => x.Id).ToList(),
            NodeTypes = cluster.NodeTypes.Select(s => s.ConvertIntToExt(projects, onlyActive))
                .ToArray()
        };
        return convert;
    }
    
    public static ExtendedClusterExt ConvertIntToExtendedExt(this Cluster cluster, IEnumerable<Project> projects, bool onlyActive)
    {
        var convert = new ExtendedClusterExt
        {
            Id = cluster.Id,
            Name = cluster.Name,
            Description = cluster.Description,
            MasterNodeName = cluster.MasterNodeName,
            SchedulerType = cluster.SchedulerType.ConvertIntToExt(),
            TimeZone = cluster.TimeZone,
            Port = cluster.Port,
            ConnectionProtocol = cluster.ConnectionProtocol.ConvertIntToExt(),
            DomainName = cluster.DomainName,
            UpdateJobStateByServiceAccount = cluster.UpdateJobStateByServiceAccount??false,
            ProxyConnection = cluster.ProxyConnection?.ConvertIntToExt(),
            FileTransferMethodIds = cluster.FileTransferMethods.Select(x => x.Id).ToList(),
            NodeTypes = cluster.NodeTypes.Select(s => s.ConvertIntToExt(projects, onlyActive))
                .ToArray(),
            CustomConfiguration = MaskCustomConfiguration(cluster),
            CustomConfigurationVaultToggles = cluster.CustomConfigurationVaultToggles
        };
        return convert;
    }

    private static Dictionary<string, string>? MaskCustomConfiguration(Cluster cluster)
    {
        var copy = cluster.CustomConfiguration != null 
            ? new Dictionary<string, string>(cluster.CustomConfiguration, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "EnableCallback", HPCConnectionFrameworkConfiguration.ScriptsSettings.EnableCallback ? "true" : "false" },
            { "EnableGracefulTimeout", HPCConnectionFrameworkConfiguration.ScriptsSettings.EnableGracefulTimeout ? "true" : "false" },
            { "GracefulTimeoutSeconds", HPCConnectionFrameworkConfiguration.ScriptsSettings.GracefulTimeoutSeconds.ToString() },
            { "SyncScriptsViaSftp", HPCConnectionFrameworkConfiguration.ScriptsSettings.SyncScriptsViaSftp ? "true" : "false" }
        };

        if (!string.IsNullOrEmpty(HPCConnectionFrameworkConfiguration.ScriptsSettings.CallbackUrl))
        {
            defaults["CallbackUrl"] = HPCConnectionFrameworkConfiguration.ScriptsSettings.CallbackUrl;
        }

        foreach (var (key, defaultValue) in defaults)
        {
            if (!copy.TryGetValue(key, out var val) || string.IsNullOrWhiteSpace(val))
            {
                copy[key] = defaultValue;
            }
        }

        if (copy.ContainsKey("ClusterCallbackNotifyToken")) copy["ClusterCallbackNotifyToken"] = "********";
        if (copy.ContainsKey("QSchedulerNotifyToken")) copy["QSchedulerNotifyToken"] = "********";

        // Mask all keys that are toggled to be stored in Vault
        if (cluster.CustomConfigurationVaultToggles != null)
        {
            foreach (var pair in cluster.CustomConfigurationVaultToggles)
            {
                if (pair.Value && copy.ContainsKey(pair.Key))
                {
                    copy[pair.Key] = "********";
                }
            }
        }

        return copy;
    }
    
    public static SchedulerTypeExt ConvertIntToExt(this SchedulerType schedulerType)
    {
        return schedulerType switch
        {
            SchedulerType.LinuxLocal => SchedulerTypeExt.LinuxLocal,
            SchedulerType.PbsPro => SchedulerTypeExt.PbsPro,
            SchedulerType.Slurm => SchedulerTypeExt.Slurm,
            SchedulerType.HyperQueue => SchedulerTypeExt.HyperQueue,
            SchedulerType.FirecRestSlurm => SchedulerTypeExt.FirecRestSlurm,
            SchedulerType.QScheduler => SchedulerTypeExt.QScheduler,
            _ => throw new InputValidationException(
                "EnumValueMustBeInInterval",
                "Scheduler type",
                $"<{string.Join(", ", Enum.GetValues(typeof(SchedulerTypeExt)).Cast<int>())}>"
            )
        };
    }
    
    public static SchedulerType ConvertExtToInt(this SchedulerTypeExt schedulerTypeExt)
    {
        return (SchedulerType)schedulerTypeExt;
    }
    
    public static ClusterConnectionProtocol ConvertExtToInt(this ClusterConnectionProtocolExt connectionProtocolExt)
    {
        return (ClusterConnectionProtocol)connectionProtocolExt;
    }
    
    public static ClusterConnectionProtocolExt ConvertIntToExt(this ClusterConnectionProtocol connectionProtocol)
    {
        return connectionProtocol switch
        {
            ClusterConnectionProtocol.None => ClusterConnectionProtocolExt.None,
            ClusterConnectionProtocol.MicrosoftHpcApi => ClusterConnectionProtocolExt.MicrosoftHpcApi,
            ClusterConnectionProtocol.Ssh => ClusterConnectionProtocolExt.Ssh,
            ClusterConnectionProtocol.SshInteractive => ClusterConnectionProtocolExt.SshInteractive,
            ClusterConnectionProtocol.Http => ClusterConnectionProtocolExt.Http,
            ClusterConnectionProtocol.Https => ClusterConnectionProtocolExt.Https,
            _ => throw new InputValidationException(
                "EnumValueMustBeInInterval",
                "Connection protocol",
                $"<{string.Join(", ", Enum.GetValues(typeof(ClusterConnectionProtocolExt)).Cast<int>())}>"
            )
        };
    }


    public static ClusterNodeTypeExt ConvertIntToExt(this ClusterNodeType nodeType)
    {
        // get all projects
        var projectExts = new List<ProjectExt>();
        if (nodeType.Cluster != null)
        {
            var dbClusterProjects = nodeType.Cluster.ClusterProjects?
                .Where(x => !x.IsDeleted && x.Project != null)
                .GroupBy(x => x.ProjectId)
                .Select(g => g.First())
                .OrderBy(o=>o.ClusterId)
                .ToList();

            if (dbClusterProjects != null)
            {
                foreach (var cp in dbClusterProjects)
                {
                    projectExts.Add(cp.Project.ConvertIntToExt());
                }
            }

            // select possible commands for specific project or command for all projects
            foreach (var project in projectExts)
                project.CommandTemplates = nodeType.PossibleCommands.Where(c =>
                        !c.IsDeleted && (!c.ProjectId.HasValue || c.ProjectId == project.Id))
                    .Select(command => command.ConvertIntToExt())
                    .ToArray();
        }

        var convert = new ClusterNodeTypeExt
        {
            Id = nodeType.Id,
            Name = nodeType.Name,
            Description = nodeType.Description,
            NumberOfNodes = nodeType.NumberOfNodes,
            CoresPerNode = nodeType.CoresPerNode,
            MaxWalltime = nodeType.MaxWalltime,
            FileTransferMethodId = nodeType.FileTransferMethodId,
            Queue = nodeType.Queue,
            QualityOfService = nodeType.QualityOfService,
            ClusterAllocationName = nodeType.ClusterAllocationName,
            ClusterNodeTypeAggregation = nodeType.ClusterNodeTypeAggregation?.ConvertIntToExt(),
            Accounting = nodeType.ClusterNodeTypeAggregation?.ClusterNodeTypeAggregationAccountings
                .Select(s => s.Accounting?.ConvertIntToExt()).ToArray(),
            ClusterId = nodeType.ClusterId,
            Projects = projectExts.ToArray()
        };
        return convert;
    }

    public static ClusterNodeTypeExt ConvertIntToExt(this ClusterNodeType nodeType, IEnumerable<Project> projects, bool onlyActive)
    {
        var safeProjects = projects?.Where(p => p != null) ?? Enumerable.Empty<Project>();
        var allowedProjectIds = new HashSet<long>(safeProjects.Select(p => p.Id));
        var projectExts = new List<ProjectExt>();

        if (nodeType.Cluster != null)
        {
            var dbClusterProjects = nodeType.Cluster.ClusterProjects?
                .Where(x => !x.IsDeleted && x.Project != null && allowedProjectIds.Contains(x.ProjectId))
                .GroupBy(x => x.ProjectId)
                .Select(g => g.First())
                .OrderBy(o => o.ClusterId)
                .ToList();

            if (dbClusterProjects != null)
            {
                foreach (var cp in dbClusterProjects)
                {
                    if (onlyActive && cp.Project.EndDate < DateTime.UtcNow) continue;

                    projectExts.Add(cp.Project.ConvertIntToExt());
                }
            }

            foreach (var project in projectExts)
            {
                project.CommandTemplates = nodeType.PossibleCommands?
                    .Where(c => !c.IsDeleted && (!c.ProjectId.HasValue || c.ProjectId == project.Id))
                    .Select(command => command.ConvertIntToExt())
                    .ToArray() ?? Array.Empty<CommandTemplateExt>();
            }
        }

        var convert = new ClusterNodeTypeExt
        {
            Id = nodeType.Id,
            Name = nodeType.Name,
            Description = nodeType.Description,
            NumberOfNodes = nodeType.NumberOfNodes,
            CoresPerNode = nodeType.CoresPerNode,
            MaxWalltime = nodeType.MaxWalltime,
            FileTransferMethodId = nodeType.FileTransferMethodId,
            Queue = nodeType.Queue,
            QualityOfService = nodeType.QualityOfService,
            ClusterAllocationName = nodeType.ClusterAllocationName,
            ClusterNodeTypeAggregation = nodeType.ClusterNodeTypeAggregation?.ConvertIntToExt(),
            Accounting = nodeType.ClusterNodeTypeAggregation?.ClusterNodeTypeAggregationAccountings?
                .Select(s => s.Accounting?.ConvertIntToExt())
                .Where(a => a != null)
                .ToArray() ?? Array.Empty<AccountingExt>(),
            ClusterId = nodeType.ClusterId,
            Projects = projectExts.ToArray()
        };

        return convert;
    }

    public static ClusterNodeTypeResourceUsageExt ConvertIntToExt(this ClusterNodeTypeResourceUsage nodeType)
    {
        var convert = new ClusterNodeTypeResourceUsageExt
        {
            Id = nodeType.Id,
            Name = nodeType.Name,
            Description = nodeType.Description,
            NumberOfNodes = nodeType.NumberOfNodes,
            CoresPerNode = nodeType.CoresPerNode,
            MaxWalltime = nodeType.MaxWalltime,
            FileTransferMethodId = nodeType.FileTransferMethod?.Id,
            NodeUsedCoresAndLimitation = nodeType.NodeUsedCoresAndLimitation.ConvertIntToExt()
        };
        return convert;
    }

    public static ClusterNodeTypeAggregationExt ConvertIntToExt(this ClusterNodeTypeAggregation aggregation)
    {
        var convert = new ClusterNodeTypeAggregationExt
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

    public static ClusterNodeTypeAggregationAccountingExt ConvertIntToExt(
        this ClusterNodeTypeAggregationAccounting accounting)
    {
        var convert = new ClusterNodeTypeAggregationAccountingExt
        {
            ClusterNodeTypeAggregationId = accounting.ClusterNodeTypeAggregationId,
            AccountingId = accounting.AccountingId
        };
        return convert;
    }

    public static CommandTemplateExt ConvertIntToExt(this CommandTemplate commandTemplate)
    {
        if (commandTemplate == null) return null;

        var convert = new CommandTemplateExt
        {
            Id = commandTemplate.Id,
            Name = commandTemplate.Name,
            Description = commandTemplate.Description,
            ExtendedAllocationCommand = commandTemplate.ExtendedAllocationCommand,
            IsGeneric = commandTemplate.IsGeneric,
            IsEnabled = commandTemplate.IsEnabled,
            CreatedFromGenericTemplateId = commandTemplate.CreatedFromId,
            TemplateParameters = commandTemplate.TemplateParameters?.Where(w => w.IsVisible)
                .Select(s => s.ConvertIntToExt())
                .ToArray()
        };
        return convert;
    }

    public static ExtendedCommandTemplateExt ConvertIntToExtendedExt(this CommandTemplate commandTemplate)
    {
        if (commandTemplate == null) return null;

        var convert = new ExtendedCommandTemplateExt
        {
            Id = commandTemplate.Id,
            Name = commandTemplate.Name,
            Description = commandTemplate.Description,
            ExtendedAllocationCommand = commandTemplate.ExtendedAllocationCommand,
            IsGeneric = commandTemplate.IsGeneric,
            IsEnabled = commandTemplate.IsEnabled,
            PreparationScript = commandTemplate.PreparationScript,
            ExecutableFile = commandTemplate.ExecutableFile,
            CommandParameters = commandTemplate.CommandParameters,
            ProjectId = commandTemplate.ProjectId.HasValue ? commandTemplate.ProjectId.Value : 0,
            ClusterNodeTypeId =
                commandTemplate.ClusterNodeTypeId.HasValue ? commandTemplate.ClusterNodeTypeId.Value : 0,
            CreatedFromGenericTemplateId = commandTemplate.CreatedFromId,
            TemplateParameters = commandTemplate.TemplateParameters?.Where(w => w.IsVisible)
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

    public static ExtendedCommandTemplateParameterExt ConvertIntToExtendedExt(
        this CommandTemplateParameter templateParameter)
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
            ClusterAuthenticationCredentialsAuthType.SshCertificate =>
                ClusterAuthenticationCredentialsAuthTypeExt.SshCertificate,
            ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy =>
                ClusterAuthenticationCredentialsAuthTypeExt.SshCertificateViaProxy,
            ClusterAuthenticationCredentialsAuthType.Kerberos =>
                ClusterAuthenticationCredentialsAuthTypeExt.Kerberos,
            ClusterAuthenticationCredentialsAuthType.FirecRestIdpViaExpirio =>
                ClusterAuthenticationCredentialsAuthTypeExt.FirecRestIdpViaExpirio,
            _ => ClusterAuthenticationCredentialsAuthTypeExt.Unknown
        };
    }

    public static ClusterAuthenticationCredentialsAuthType ConvertExtToInt(
        this ClusterAuthenticationCredentialsAuthTypeExt type)
    {
        _ = Enum.TryParse(type.ToString(), out ClusterAuthenticationCredentialsAuthType convert);
        return convert;
    }

    #endregion
}