using HEAppE.BusinessLogicTier.Logic;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.DomainObjects.UserAndLimitationManagement.Wrapper;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.OpenStackAPI.DTO;
using System.Linq;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Converts
{
    public static class UserAndLimitationManagementConverts
    {
        public static AdaptorUserExt ConvertIntToExt(this AdaptorUser user)
        {
            var convert = new AdaptorUserExt()
            {
                Id = user.Id,
                Username = user.Username
            };
            return convert;
        }

        public static AdaptorUserGroupExt ConvertIntToExt(this AdaptorUserGroup userGroup)
        {
            var convert = new AdaptorUserGroupExt()
            {
                Id = userGroup.Id,
                Name = userGroup.Name,
                Description = userGroup.Description,
                Project = userGroup.Project?.ConvertIntToExt(),
                Users = userGroup.Users.Select(s => s.ConvertIntToExt())
                                        .ToArray()
            };
            return convert;
        }

        public static ResourceUsageExt ConvertIntToExt(this ResourceUsage usage)
        {
            var convert = new ResourceUsageExt()
            {
                NodeType = usage.NodeType.ConvertIntToExt(),
                CoresUsed = usage.CoresUsed,
                Limitation = usage.Limitation.ConvertIntToExt()
            };
            return convert;
        }

        public static ResourceLimitationExt ConvertIntToExt(this ResourceLimitation resourceLimitation)
        {
            if (resourceLimitation != null)
            {
                return new ResourceLimitationExt()
                {
                    TotalMaxCores = resourceLimitation.TotalMaxCores,
                    MaxCoresPerJob = resourceLimitation.MaxCoresPerJob
                };
            }
            else
            {
                return new ResourceLimitationExt();
            }

        }

        public static FileTransferKeyCredentialsExt ConvertIntToExt(this AuthenticationCredentials credentials)
        {
            if (credentials is FileTransferKeyCredentials asymmetricKeyCredentials)
            {
                var convert = new FileTransferKeyCredentialsExt()
                {
                    Username = asymmetricKeyCredentials.Username,
                    Password = asymmetricKeyCredentials.Password,
                    CipherType = ConvertFileTransferMethodIntToExt(asymmetricKeyCredentials.FileTransferCipherType),
                    PrivateKey = asymmetricKeyCredentials.PrivateKey,
                    PublicKey = asymmetricKeyCredentials.PublicKey

                };
                return convert;
            }
            else
            {
                return new FileTransfer.Models.FileTransferKeyCredentialsExt();
            }
        }

        public static FileTransferKeyCredentials ConvertExtToInt(this AuthenticationCredentialsExt credentials)
        {
            FileTransferKeyCredentialsExt asymmetricKeyCredentials = credentials as FileTransferKeyCredentialsExt;
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

        public static OpenStackApplicationCredentialsExt ConvertIntToExt(this ApplicationCredentialsDTO applicationCredentialsDto)
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
            var convert = new NodeUsedCoresAndLimitationExt()
            {
                CoresUsed = usedCoresAndLimitations.CoresUsed,
                Limitation = usedCoresAndLimitations.Limitation.ConvertIntToExt()
            };
            return convert;
        }

        private static FileTransferCipherTypeExt ConvertFileTransferMethodIntToExt(FileTransferCipherType fileTransferMethod)
        {
            return fileTransferMethod switch
            {
                FileTransferCipherType.RSA3072 => FileTransferCipherTypeExt.RSA3072,
                FileTransferCipherType.RSA4096 => FileTransferCipherTypeExt.RSA4096,
                FileTransferCipherType.nistP256 => FileTransferCipherTypeExt.nistP256,
                FileTransferCipherType.nistP521 => FileTransferCipherTypeExt.nistP521,
                FileTransferCipherType.Ed25519 => FileTransferCipherTypeExt.Ed25519,
                _ => FileTransferCipherTypeExt.RSA4096
            };
        }

        private static FileTransferCipherType ConvertFileTransferMethodExtToInt(FileTransferCipherTypeExt? fileTransferMethod)
        {
            if (!fileTransferMethod.HasValue)
            {
                throw new InputValidationException("The file transfer method has to be set.");
            }

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
}