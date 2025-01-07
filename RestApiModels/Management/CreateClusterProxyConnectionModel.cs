using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "CreateClusterProxyConnectionModel")]
public class CreateClusterProxyConnectionModel : SessionCodeModel
{
    [DataMember(Name = "Host", IsRequired = true)]
    [StringLength(40)]
    public string Host { get; set; }

    [DataMember(Name = "Port", IsRequired = true)]
    public int Port { get; set; }

    [DataMember(Name = "Username", IsRequired = true)]
    [StringLength(50)]
    public string Username { get; set; }

    [DataMember(Name = "Password", IsRequired = true)]
    [StringLength(50)]
    public string Password { get; set; }

    [DataMember(Name = "Type", IsRequired = true)]
    public ProxyType Type { get; set; }
}