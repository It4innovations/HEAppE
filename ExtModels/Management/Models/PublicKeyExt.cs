using HEAppE.DomainObjects.FileTransfer;
using HEAppE.ExtModels.FileTransfer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.ExtModels.Management.Models
{
    public class PublicKeyExt
    {
        public FileTransferCipherTypeExt KeyType { get; set; }
        public string PublicKeyOpenSSH { get; set; }
        public string PublicKeyPEM { get; set; }
    }
}
