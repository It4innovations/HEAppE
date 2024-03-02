using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Internal;
using log4net;
namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH
{
    /// <summary>
    ///  Ssh command utils
    /// </summary>
    internal static class SshCommandUtils
    {
        #region Properties
        /// <summary>
        /// Log4Net logger
        /// </summary>
        private static readonly ILog _log;
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
            SshCommandWrapper sshCommand = client.RunCommand(command);
            if (sshCommand.ExitStatus != 0)
            {
                if (sshCommand.Error.Contains("No such file or directory"))
                {
                    _log.Warn($"SSH command error: {sshCommand.Error} Error code: {sshCommand.ExitStatus} SSH command: {sshCommand.CommandText}");
                    throw new InputValidationException("NoFileOrDirectory");
                }

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
