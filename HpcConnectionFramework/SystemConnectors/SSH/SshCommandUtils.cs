using log4net;
using Renci.SshNet;
using System;
namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH
{
    /// <summary>
    /// Class: Ssh command utls
    /// </summary>
    internal static class SshCommandUtils
    {
        #region Properties
        /// <summary>
        /// Log4Net logger
        /// </summary>
        static ILog _log;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        static SshCommandUtils()
        {
            _log = LogManager.GetLogger(typeof(SshCommandUtils));
        }
        #endregion
        #region Methods
        /// <summary>
        /// Method: Run ssh command
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="command">Command</param>
        /// <returns></returns>
        internal static SshCommandWrapper RunSshCommand(SshClientAdapter client, string command)
        {
            var sshCommand = client.RunCommand(command);
            if (sshCommand.ExitStatus != 0)
            {
                throw new SshCommandException($"SSH command error: {sshCommand.Error} Error code: {sshCommand.ExitStatus} SSH command: {sshCommand.CommandText}");
            }

            if (sshCommand.Error.Length > 0)
            {
                _log.WarnFormat("SSH command finished with error: {0}", sshCommand.Error);
            }

            return sshCommand;
        }
        #endregion
    }
}
