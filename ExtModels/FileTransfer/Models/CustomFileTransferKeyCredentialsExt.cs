using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.FileTransfer.Models
{
    [DataContract(Name = "AuthenticationHPCCredentialsExt")]
    public class CustomFileTransferKeyCredentialsExt
    {
        [DataMember(Name = "UserName"), StringLength(50)]
        public string Username { get; set; }

        [DataMember(Name = "Password")]
        public string Password { get; set; }

        [DataMember(Name = "PrivateKey")]
        public string PrivateKey { get; set; }

        [DataMember(Name = "CipherType")]
        public FileTransferCipherTypeExt? CipherType { get; set; }

        public override string ToString()
        {
            return $"""AuthenticationHPCCredentialsExt(UserName="{Username}"; Password="{Password}"; PrivateKey="{PrivateKey}"; CipherType="{CipherType}")""";
        }
    }
}
