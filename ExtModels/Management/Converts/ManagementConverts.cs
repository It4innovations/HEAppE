﻿using System;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.Management;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.FileTransfer.Models;
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

    public static ClusterInitReportExt ConvertIntToExt(this ClusterInitReport report)
    {
        var convert = new ClusterInitReportExt
        {
            ClusterName = report.Cluster.Name,
            IsClusterInitialized = report.IsClusterInitialized
        };
        return convert;
    }

    public static ClusterProjectExt ConvertIntToExt(this ClusterProject cp)
    {
        var convert = new ClusterProjectExt
        {
            ClusterId = cp.ClusterId,
            ProjectId = cp.ProjectId,
            LocalBasepath = cp.LocalBasepath,
            CreatedAt = cp.CreatedAt,
            ModifiedAt = cp.ModifiedAt
        };
        return convert;
    }

    #endregion
}