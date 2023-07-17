using HEAppE.ExtModels.FileTransfer.Models;

namespace HEAppE.ExtModels.Management.Models
{
    public class PublicKeyExt
    {
        public FileTransferCipherTypeExt KeyType { get; set; }
        public string PublicKeyOpenSSH { get; set; }
        public string PublicKeyPEM { get; set; }
    }
}
