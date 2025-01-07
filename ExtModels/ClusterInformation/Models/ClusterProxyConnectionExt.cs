using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

[DataContract(Name = "ClusterProxyConnectionExt")]
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

    [DataMember(Name = "Id")] public long Id { get; set; }

    [DataMember(Name = "Host")] public string Host { get; set; }

    [DataMember(Name = "Port")] public int Port { get; set; }

    [DataMember(Name = "Type")] public ProxyTypeExt Type { get; set; }

    [DataMember(Name = "Username", IsRequired = false, EmitDefaultValue = false)]
    public string Username { get; set; }

    [DataMember(Name = "Password", IsRequired = false, EmitDefaultValue = false)]
    public string Password { get; set; }

    #endregion
}