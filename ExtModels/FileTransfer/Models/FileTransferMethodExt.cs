﻿using HEAppE.ExtModels.ClusterInformation.Models;
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

        [DataMember(Name = "SharedBasepath"), StringLength(200)]
        public string SharedBasepath { get; set; }

        [DataMember(Name = "Protocol")]
        public FileTransferProtocolExt? Protocol { get; set; }

        [DataMember(Name = "ProxyConnection")]
        public ClusterProxyConnectionExt ProxyConnection { get; set; }

        [DataMember(Name = "Credentials")]
        public AsymmetricKeyCredentialsExt Credentials { get; set; }

        public override string ToString()
        {
            return $"FileTransferMethodExt(ServerHostname={ServerHostname}; SharedBasepath={SharedBasepath}; Protocol={Protocol}; Credentials={Credentials}; ProxyConnection={ProxyConnection})";
        }
    }
}
