using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.FileTransfer.Models
{
    [DataContract(Name = "FileTransferKeyCredentialsExt")]
    public class FileTransferKeyCredentialsExt : AuthenticationCredentialsExt
    {
        [DataMember(Name = "Password")]
        public string Password { get; set; }

        [DataMember(Name = "CipherType")]
        public FileTransferCipherTypeExt? CipherType { get; set; }

        [DataMember(Name = "PrivateKey")]
        public string PrivateKey { get; set; }

        [DataMember(Name = "PublicKey")]
        public string PublicKey { get; set; }

        [DataMember(Name = "Passphrase")]
        public string Passphrase { get; set; }
        public override string ToString()
        {
            return $"""AsymmetricKeyCredentialsExt(Username="{Username}";CipherType="{CipherType}"; PublicKey="{PublicKey}; Passphrase={Passphrase}")""";
        }
    }
}
