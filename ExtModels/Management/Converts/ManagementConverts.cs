using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.Management;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.Management.Models;

namespace HEAppE.ExtModels.Management.Converts;

public static class ManagementConverts
{
    #region Public Methods

    public static UsageType ConvertExtToInt(this UsageTypeExt? usageType)
    {
        switch (usageType)
        {
            case UsageTypeExt.CoreHours:
                return UsageType.CoreHours;
            case UsageTypeExt.NodeHours:
                return UsageType.NodeHours;
            case UsageTypeExt.Credits:
                return UsageType.Credits;
            case UsageTypeExt.QPUSeconds:
                return UsageType.QPUSeconds;
            default:
                return UsageType.CoreHours;
        }
    }

    public static DeploymentTypeExt ConvertIntToExt(this DeploymentType type)
    {
        _ = Enum.TryParse(type.ToString(), out DeploymentTypeExt convert);
        return convert;
    }

    public static ResourceAllocationTypeExt ConvertIntToExt(this ResourceAllocationType type)
    {
        _ = Enum.TryParse(type.ToString(), out ResourceAllocationTypeExt convert);
        return convert;
    }

    public static FileTransferCipherTypeExt ConvertIntToExt(this FileTransferCipherType type)
    {
        switch (type)
        {
            case FileTransferCipherType.RSA3072:
                return FileTransferCipherTypeExt.RSA3072;
            case FileTransferCipherType.RSA4096:
                return FileTransferCipherTypeExt.RSA4096;
            case FileTransferCipherType.nistP256:
                return FileTransferCipherTypeExt.nistP256;
            case FileTransferCipherType.nistP521:
                return FileTransferCipherTypeExt.nistP521;
            case FileTransferCipherType.Ed25519:
                return FileTransferCipherTypeExt.Ed25519;
            case FileTransferCipherType.Unknown:
                return FileTransferCipherTypeExt.None;
            default:
                throw new ArgumentException($"Unknown FileTransferCipherType: {type}");
        }
    }

    public static PublicKeyExt ConvertIntToExt(this SecureShellKey key)
    {
        var convert = new PublicKeyExt
        {
            KeyType = key.CipherType.ConvertIntToExt(),
            PublicKeyOpenSSH = key.PublicKeyInAuthorizedKeysFormat,
            PublicKeyPEM = key.PublicKeyPEM,
            Username = key.Username
        };
        return convert;
    }

    public static CredentialResponseExt ConvertIntToExt(this CredentialResponse credential)
    {
        var convert = new CredentialResponseExt
        {
            Id = credential.Id,
            Username = credential.Username,
            AuthType = credential.AuthType,
            IsGenerated = credential.IsGenerated,
            PublicKeyFingerprint = credential.PublicKeyFingerprint,
            PublicKeyExt = credential.PublicKeyExt,
            AdaptorUserId = credential.AdaptorUserId,
        };
        return convert;
    }

    public static ClusterInitReportExt ConvertIntToExt(this ClusterInitReport report)
    {
        var convert = new ClusterInitReportExt
        {
            ClusterName = report.Cluster.Name,
            IsClusterInitialized = report.IsClusterInitialized
        };
        return convert;
    }
    
    public static ClusterAccessReportExt ConvertIntToExt(this ClusterAccessReport report)
    {
        var convert = new ClusterAccessReportExt
        {
            ClusterName = report.Cluster.Name,
            IsClusterAccessible = report.IsClusterAccessible
        };
        return convert;
    }
    
    public static ClusterAccountStatusExt ConvertIntToExt(this ClusterAccountStatus status, IEnumerable<Project> projects, bool onlyActive)
    {
        var convert = new ClusterAccountStatusExt
        {
            Cluster = status.Cluster.ConvertIntToExt(projects, onlyActive),
            Project = status.Project.ConvertIntToExt(),
            IsInitialized = status.IsInitialized
            
        };
        return convert;
    }

    public static ClusterProjectExt ConvertIntToExt(this ClusterProject cp)
    {
        var convert = new ClusterProjectExt
        {
            ClusterId = cp.ClusterId,
            ProjectId = cp.ProjectId,
            ScratchStoragePath = cp.ScratchStoragePath,
            ProjectStoragePath = cp.ProjectStoragePath,
            CreatedAt = cp.CreatedAt,
            ModifiedAt = cp.ModifiedAt,
            PreferredAuthType = cp.PreferredAuthType.ConvertIntToExt(),
            AdaptorUserId = cp.ClusterProjectCredentials?.FirstOrDefault(x => !x.IsDeleted)?.AdaptorUserId
        };
        return convert;
    }

    public static JobExternalServiceLogExt ConvertIntToExt(this HEAppE.DomainObjects.Monitoring.ExternalServiceHealthLog log)
    {
        return new JobExternalServiceLogExt
        {
            Id = log.Id,
            ServiceName = log.ServiceName,
            ServiceType = log.ServiceType,
            Protocol = log.Protocol,
            EndpointOrHost = log.EndpointOrHost,
            Port = log.Port,
            Operation = log.CommandOrPath,
            Timestamp = log.Timestamp,
            IsSuccess = log.IsAvailable,
            ResponseTimeMs = log.ResponseTimeMs,
            StatusCode = log.StatusCode,
            ErrorMessage = log.ErrorMessage,
            JobId = log.JobId,
            TaskId = log.TaskId,
            ClusterId = log.ClusterId,
            RequestId = log.RequestId,
            Source = log.Source
        };
    }

    public static ExternalServiceStatisticsExt ConvertIntToExt(this HEAppE.DomainObjects.Monitoring.ExternalServiceStatistics stats)
    {
        return new ExternalServiceStatisticsExt
        {
            ServiceName = stats.ServiceName,
            ServiceType = stats.ServiceType,
            CommandOrPath = stats.CommandOrPath,
            AvailabilityPercentage = stats.AvailabilityPercentage,
            AverageResponseTimeMs = stats.AverageResponseTimeMs,
            MinResponseTimeMs = stats.MinResponseTimeMs,
            MaxResponseTimeMs = stats.MaxResponseTimeMs,
            P95ResponseTimeMs = stats.P95ResponseTimeMs,
            TotalChecks = stats.TotalChecks,
            FailedChecks = stats.FailedChecks
        };
    }

    public static ExternalServiceLiveStatusExt ConvertIntToExt(this HEAppE.DomainObjects.Monitoring.ExternalServiceLiveStatus status)
    {
        return new ExternalServiceLiveStatusExt
        {
            ServiceName = status.ServiceName,
            Type = status.Type,
            Protocol = status.Protocol,
            EndpointOrHost = status.EndpointOrHost,
            Port = status.Port,
            IsAvailable = status.IsAvailable,
            ResponseTimeMs = status.ResponseTimeMs,
            ErrorMessage = status.ErrorMessage,
            LastCheck = status.LastCheck
        };
    }

    public static ExternalServicesReportExt ConvertIntToExt(this HEAppE.DomainObjects.Monitoring.ExternalServicesReport report)
    {
        return new ExternalServicesReportExt
        {
            LiveStatus = report.LiveStatus?.Select(x => x.ConvertIntToExt()).ToList(),
            Statistics = report.Statistics?.Select(x => x.ConvertIntToExt()).ToList()
        };
    }

    public static SystemRoleAssignmentExt ConvertIntToExt(this SystemRoleAssignment assignment)
    {
        if (assignment == null) return null;
        return new SystemRoleAssignmentExt
        {
            Username = assignment.Username,
            Role = assignment.Role.ToString(),
            Source = assignment.Source
        };
    }

    #endregion
}