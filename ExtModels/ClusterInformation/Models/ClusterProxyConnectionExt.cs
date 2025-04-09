using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Cluster proxy connection ext
/// </summary>
[DataContract(Name = "ClusterProxyConnectionExt")]
[Description("Cluster proxy connection ext")]
public class ClusterProxyConnectionExt
{
    #region Override Methods

    public override string ToString()
    {
        return
            $"ClusterProxyConnectionExt: Host={Host}, Type={Type}, Port={Port}, Username={Username}, Password={Password}";
    }

    #endregion

    #region Properties

    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Host
    /// </summary>
    [DataMember(Name = "Host")]
    [Description("Host")]
    public string Host { get; set; }

    /// <summary>
    /// Port number
    /// </summary>
    [DataMember(Name = "Port")]
    [Description("Port number")]
    public int Port { get; set; }

    /// <summary>
    /// Proxy type
    /// </summary>
    [DataMember(Name = "Type")]
    [Description("Proxy type")]
    public ProxyTypeExt Type { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    [DataMember(Name = "Username", IsRequired = false, EmitDefaultValue = false)]
    [Description("Username")]
    public string Username { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    [DataMember(Name = "Password", IsRequired = false, EmitDefaultValue = false)]
    [Description("Password")]
    public string Password { get; set; }

    #endregion
}