using Exceptions.Internal;
using log4net;
namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH
{
    /// <summary>
    ///  Ssh command utls
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
        /// Run ssh command
        /// </summary>
        /// <param name="client">Client</param>
        /// <param name="command">Command</param>
        /// <returns></returns>
        internal static SshCommandWrapper RunSshCommand(SshClientAdapter client, string command)
        {
            var sshCommand = client.RunCommand(command);
            if (sshCommand.ExitStatus != 0)
            {
                _log.Error($"SSH command error: {sshCommand.Error} Error code: {sshCommand.ExitStatus} SSH command: {sshCommand.CommandText}");
                throw new SshCommandException("CommandException", sshCommand.Error, sshCommand.ExitStatus, sshCommand.CommandText);
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
