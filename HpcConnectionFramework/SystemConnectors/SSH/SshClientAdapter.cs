using System;
using Renci.SshNet;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH
{
    public class SshClientAdapter
    {
        private readonly SshClient _sshClient;


        public SshClientAdapter(SshClient sshClient)
        {
            _sshClient = sshClient;
        }

        public SshCommandWrapper RunCommand(string command)
        {
            if (_sshClient is NoAuthenticationSshClient ownSshCommand)
            {
                return ownSshCommand.RunShellCommand(command);
            }
            else
            {
                return new SshCommandWrapper(_sshClient.RunCommand(command));
            }
        }

        internal void Connect()
        {
            if (!(_sshClient is NoAuthenticationSshClient))
            {
                _sshClient.Connect();
            }
        }

        internal void Disconnect()
        {
            if (!(_sshClient is NoAuthenticationSshClient))
            {
                _sshClient.Disconnect();
            }
        }
    }
}
