using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.Utils
{
    public class ConnectionConfiguration
    {
        public string Host { get; set;}
        public int Port { get; set;} = 22;
    }
    public class ConnectionConfigurationUtils
    {
        /// <summary>
        /// Parse host and port to object
        /// </summary>
        /// <param name="hostWithPort">host:port</param>
        /// <returns></returns>
        public static ConnectionConfiguration ParseConnectionConfiguration(string hostWithPort)
        {
            var hostPort = hostWithPort.Split(':');
            int port = int.Parse(hostPort[1] ?? "22");
            return new ConnectionConfiguration()
            {
                Host = hostPort[0] ?? "localhost",
                Port = port,
            };
        }
    }
}
