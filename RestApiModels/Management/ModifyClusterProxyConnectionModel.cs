using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Modify cluster proxy connection model
/// </summary>
[DataContract(Name = "ModifyClusterProxyConnectionModel")]
[Description("Modify cluster proxy connection model")]
public class ModifyClusterProxyConnectionModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Host
    /// </summary>
    [DataMember(Name = "Host", IsRequired = true)]
    [StringLength(40)]
    [Description("Host")]
    public string Host { get; set; }

    /// <summary>
    /// Port
    /// </summary>
    [DataMember(Name = "Port", IsRequired = true)]
    [Description("Port")]
    public int Port { get; set; }

    /// <summary>
    /// User name
    /// </summary>
    [DataMember(Name = "Username", IsRequired = true)]
    [StringLength(50)]
    [Description("User name")]
    public string Username { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    [DataMember(Name = "Password", IsRequired = true)]
    [StringLength(50)]
    [Description("Password")]
    public string Password { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    [DataMember(Name = "Type", IsRequired = true)]
    [Description("Type")]
    public ProxyType Type { get; set; }
}