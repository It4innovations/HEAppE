using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.FileTransfer.Models
{
    [DataContract(Name = "FileTransferMethodExt")]
    public class FileTransferMethodExt
    {
        [DataMember(Name = "ServerHostname")]
        public string ServerHostname { get; set; }

        [DataMember(Name = "SharedBasepath"), StringLength(50)]
        public string SharedBasepath { get; set; }

        [DataMember(Name = "Protocol")]
        public FileTransferProtocolExt? Protocol { get; set; }

        [DataMember(Name = "Credentials")]
        public AsymmetricKeyCredentialsExt Credentials { get; set; }

        public override string ToString()
        {
            return $"FileTransferMethodExt(serverHostname={ServerHostname}; sharedBasepath={SharedBasepath}; protocol={Protocol}; credentials={Credentials})";
        }
    }
}
