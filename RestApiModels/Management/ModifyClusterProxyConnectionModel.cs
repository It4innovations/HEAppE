﻿using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "ModifyClusterProxyConnectionModel")]
    public class ModifyClusterProxyConnectionModel : SessionCodeModel
    {
        [DataMember(Name = "Id", IsRequired = true)]
        public long Id { get; set; }

        [DataMember(Name = "Host", IsRequired = true), StringLength(40)]
        public string Host { get; set; }

        [DataMember(Name = "Port", IsRequired = true)]
        public int Port { get; set; }

        [DataMember(Name = "Username", IsRequired = true), StringLength(50)]
        public string Username { get; set; }

        [DataMember(Name = "Password", IsRequired = true), StringLength(50)]
        public string Password { get; set; }

        [DataMember(Name = "Type", IsRequired = true)]
        public ProxyType Type { get; set; }
    }
}
