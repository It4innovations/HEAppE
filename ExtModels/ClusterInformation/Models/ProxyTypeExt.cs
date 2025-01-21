using System.ComponentModel;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Proxy types
/// </summary>
[Description("Proxy types")]
public enum ProxyTypeExt
{
    Socks4 = 1,
    Socks5 = 2,
    Http = 3
}