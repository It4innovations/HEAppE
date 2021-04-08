using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System.Linq;
using HEAppE.OpenStackAPI.DTO;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Converts
{
    public static class UserAndLimitationManagementConverts
    {
        public static AdaptorUserExt ConvertIntToExt(this AdaptorUser user)
        {
            AdaptorUserExt convert = new AdaptorUserExt()
            {
                Id = user.Id,
                Username = user.Username
            };
            return convert;
        }

        public static AdaptorUserGroupExt ConvertIntToExt(this AdaptorUserGroup userGroup)
        {
            AdaptorUserGroupExt convert = new AdaptorUserGroupExt()
            {
                Id = userGroup.Id,
                Name = userGroup.Name,
                Description = userGroup.Description,
                AccountingString = userGroup.AccountingString,
                Users = userGroup.Users.Select(s => s.ConvertIntToExt())
                                        .ToArray()
            };
            return convert;
        }

        public static ResourceUsageExt ConvertIntToExt(this ResourceUsage usage)
        {
            ResourceUsageExt convert = new ResourceUsageExt()
            {
                NodeType = usage.NodeType.ConvertIntToExt(),
                CoresUsed = usage.CoresUsed,
                Limitation = usage.Limitation.ConvertIntToExt()
            };
            return convert;
        }

        private static ResourceLimitationExt ConvertIntToExt(this ResourceLimitation resourceLimitation)
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

        public static AsymmetricKeyCredentialsExt ConvertIntToExt(this AuthenticationCredentials credentials)
        {
            if (credentials is AsymmetricKeyCredentials asymmetricKeyCredentials)
            {
                AsymmetricKeyCredentialsExt convert = new AsymmetricKeyCredentialsExt()
                {
                    Username = credentials.Username,
                    PrivateKey = asymmetricKeyCredentials.PrivateKey,
                    PublicKey = asymmetricKeyCredentials.PublicKey
                };
                return convert;
            }
            else
            {
                return new AsymmetricKeyCredentialsExt();
            }
        }

        public static AsymmetricKeyCredentials ConvertExtToInt(this AuthenticationCredentialsExt credentials)
        {
            AsymmetricKeyCredentialsExt asymmetricKeyCredentials = credentials as AsymmetricKeyCredentialsExt;
            AsymmetricKeyCredentials convert = new AsymmetricKeyCredentials
            {
                Username = credentials.Username,
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
    }
}