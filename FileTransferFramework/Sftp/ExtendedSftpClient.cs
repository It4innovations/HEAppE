using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.FileTransferFramework.Sftp
{
    public class ExtendedSftpClient : SftpClient
    {
        #region Instances
        /// <summary>
        /// Remote host time zone
        /// </summary>
        protected readonly string _hostTimeZone;
        #endregion
        #region Constructors
        public ExtendedSftpClient(ConnectionInfo connectionInfo, string hostTimeZone)
            : base(connectionInfo)
        {
            _hostTimeZone = hostTimeZone;
        }

        public ExtendedSftpClient(string host, string username, string password, string hostTimeZone)
            : base(host, username, password)
        {
            _hostTimeZone = hostTimeZone;
        }

        public ExtendedSftpClient(string host, string username, string hostTimeZone, params PrivateKeyFile[] keyFiles)
            : base(host, username, keyFiles)
        {
            _hostTimeZone = hostTimeZone;
        }

        public ExtendedSftpClient(string host, int port, string username, string password, string hostTimeZone)
            : base(host, port, username, password)
        {
            _hostTimeZone = hostTimeZone;
        }

        public ExtendedSftpClient(string host, int port, string username, string hostTimeZone, params PrivateKeyFile[] keyFiles)
            : base(host, port, username, keyFiles)
        {
            _hostTimeZone = hostTimeZone;
        }
        #endregion
        #region Methods
        /// <summary>
        /// Get Time zone information
        /// </summary>
        /// <returns></returns>
        public string GetTimeZone()
        {
            return _hostTimeZone;
        }
        #endregion
    }
}
