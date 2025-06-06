﻿using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.DomainObjects.UserAndLimitationManagement.Wrapper;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.OpenStackAPI.DTO;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Converts;

public static class UserAndLimitationManagementConverts
{
    public static AdaptorUserExt ConvertIntToExt(this AdaptorUser user)
    {
        var convert = new AdaptorUserExt
        {
            Id = user.Id,
            Username = user.Username,
            PublicKey = user.PublicKey,
            Email = user.Email,
            UserType = (AdaptorUserTypeExt)user.UserType,
            AdaptorUserGroups = user.Groups?.DistinctBy(g => g.Id).Select(g => g.ConvertIntToExt())
                .ToArray()
        };
        return convert;
    }

    public static AdaptorUserGroupExt ConvertIntToExt(this AdaptorUserGroup userGroup)
    {
        var convert = new AdaptorUserGroupExt
        {
            Id = userGroup.Id,
            Name = userGroup.Name,
            Description = userGroup.Description,
            Project = new ProjectExt { Name = userGroup.Project?.Name, Description = userGroup.Project?.Description },
            Roles = userGroup.AdaptorUserUserGroupRoles?.Select(r => r.AdaptorUserRole.Name)
                .ToArray()
        };
        return convert;
    }

    public static ResourceUsageExt ConvertIntToExt(this ResourceUsage usage, IEnumerable<Project> projects, bool onlyActive)
    {
        var convert = new ResourceUsageExt
        {
            NodeType = usage.NodeType.ConvertIntToExt(projects, onlyActive),
            CoresUsed = usage.CoresUsed
        };
        return convert;
    }

    public static FileTransferKeyCredentialsExt ConvertIntToExt(this AuthenticationCredentials credentials)
    {
        if (credentials is FileTransferKeyCredentials asymmetricKeyCredentials)
        {
            var convert = new FileTransferKeyCredentialsExt
            {
                Username = asymmetricKeyCredentials.Username,
                Password = asymmetricKeyCredentials.Password,
                CipherType = ConvertFileTransferMethodIntToExt(asymmetricKeyCredentials.FileTransferCipherType),
                CredentialsAuthType = asymmetricKeyCredentials.CredentialsAuthType.ConvertIntToExt(),
                PrivateKey = asymmetricKeyCredentials.PrivateKey,
                PublicKey = asymmetricKeyCredentials.PublicKey,
                PrivateKeyCertificate = asymmetricKeyCredentials.PrivateKeyCertificate,
                Passphrase = asymmetricKeyCredentials.Passphrase
            };
            return convert;
        }

        return new FileTransferKeyCredentialsExt();
    }

    public static FileTransferKeyCredentials ConvertExtToInt(this AuthenticationCredentialsExt credentials)
    {
        var asymmetricKeyCredentials = credentials as FileTransferKeyCredentialsExt;
        var convert = new FileTransferKeyCredentials
        {
            Username = asymmetricKeyCredentials.Username,
            FileTransferCipherType = ConvertFileTransferMethodExtToInt(asymmetricKeyCredentials.CipherType),
            Password = asymmetricKeyCredentials.Password,
            PrivateKey = asymmetricKeyCredentials.PrivateKey,
            PublicKey = asymmetricKeyCredentials.PublicKey
        };
        return convert;
    }

    public static OpenStackApplicationCredentialsExt ConvertIntToExt(
        this ApplicationCredentialsDTO applicationCredentialsDto)
    {
        return new OpenStackApplicationCredentialsExt
        {
            ApplicationCredentialsId = applicationCredentialsDto.ApplicationCredentialsId,
            ApplicationCredentialsSecret = applicationCredentialsDto.ApplicationCredentialsSecret
        };
    }

    public static AdaptorUserRoleExt ConvertIntToExt(this AdaptorUserRole userRole)
    {
        return new AdaptorUserRoleExt
        {
            Name = userRole.Name,
            Description = userRole.Description
        };
    }

    public static ProjectReferenceExt ConvertIntToExt(this ProjectReference projectReference)
    {
        return new ProjectReferenceExt
        {
            Project = projectReference.Project.ConvertIntToExt(),
            Role = projectReference.Role.ConvertIntToExt()
        };
    }

    public static NodeUsedCoresAndLimitationExt ConvertIntToExt(this NodeUsedCoresAndLimitation usedCoresAndLimitations)
    {
        var convert = new NodeUsedCoresAndLimitationExt
        {
            CoresUsed = usedCoresAndLimitations.CoresUsed
        };
        return convert;
    }

    private static FileTransferCipherTypeExt ConvertFileTransferMethodIntToExt(
        FileTransferCipherType fileTransferMethod)
    {
        return fileTransferMethod switch
        {
            FileTransferCipherType.RSA3072 => FileTransferCipherTypeExt.RSA3072,
            FileTransferCipherType.RSA4096 => FileTransferCipherTypeExt.RSA4096,
            FileTransferCipherType.nistP256 => FileTransferCipherTypeExt.nistP256,
            FileTransferCipherType.nistP521 => FileTransferCipherTypeExt.nistP521,
            FileTransferCipherType.Ed25519 => FileTransferCipherTypeExt.Ed25519,
            FileTransferCipherType.Unknown => FileTransferCipherTypeExt.None,
            _ => FileTransferCipherTypeExt.RSA4096
        };
    }

    private static FileTransferCipherType ConvertFileTransferMethodExtToInt(
        FileTransferCipherTypeExt? fileTransferMethod)
    {
        if (!fileTransferMethod.HasValue) throw new InputValidationException("The file transfer method has to be set.");

        return fileTransferMethod switch
        {
            FileTransferCipherTypeExt.RSA3072 => FileTransferCipherType.RSA3072,
            FileTransferCipherTypeExt.RSA4096 => FileTransferCipherType.RSA4096,
            FileTransferCipherTypeExt.nistP256 => FileTransferCipherType.nistP256,
            FileTransferCipherTypeExt.nistP521 => FileTransferCipherType.nistP521,
            FileTransferCipherTypeExt.Ed25519 => FileTransferCipherType.Ed25519,
            _ => FileTransferCipherType.RSA4096
        };
    }
}