using HEAppE.DomainObjects.ClusterInformation;
using System;

namespace HEAppE.ConnectionPool
{
    public class ConnectionInfo
    {
        public object Connection { get; set; }

        public DateTime LastUsed { get; set; }

        public ClusterAuthenticationCredentials AuthCredentials { get; set; }
    }
}