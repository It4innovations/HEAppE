using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.Management;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.Management.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.ExtModels.Management.Converts
{
    public static class ManagementConverts
    {
        #region Public Methods
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
                default:
                    throw new ArgumentException($"Unknown FileTransferCipherType: {type}");
            }
        }

        public static PublicKeyExt ConvertIntToExt(this SecureShellKey key)
        {
            var convert = new PublicKeyExt()
            {
                KeyType = key.CipherType.ConvertIntToExt(),
                PublicKeyOpenSSH = key.PublicKeyInAuthorizedKeysFormat,
                PublicKeyPEM = key.PublicKeyPEM
            };
            return convert;
        }
        #endregion
    }
}
