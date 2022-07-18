using HEAppE.DomainObjects.ClusterInformation;
using Renci.SshNet;

namespace HEAppE.Utils
{
    public static class Mapper
    {
        public static ProxyTypes Map(this ProxyType type)
        {
            return type switch
            {
                ProxyType.Socks5 => ProxyTypes.Socks5,
                ProxyType.Socks4 => ProxyTypes.Socks4,
                ProxyType.Http => ProxyTypes.Http,
                _ => ProxyTypes.None,
            };
        }
    }
}
