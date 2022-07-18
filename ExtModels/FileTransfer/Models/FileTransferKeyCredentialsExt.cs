using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.FileTransfer.Models
{
    [DataContract(Name = "FileTransferKeyCredentialsExt")]
    public class FileTransferKeyCredentialsExt : AuthenticationCredentialsExt
    {
        [DataMember(Name = "PrivateKey")]
        public string PrivateKey { get; set; }

        [DataMember(Name = "PublicKey")]
        public string PublicKey { get; set; }

        public override string ToString()
        {
            return $"AsymmetricKeyCredentialsExt({base.ToString()}; privateKey={PrivateKey}; publicKey={PublicKey})";
        }
    }
}
