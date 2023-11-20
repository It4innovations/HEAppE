using HEAppE.Exceptions.Internal;
using log4net;
using Renci.SshNet;
using System;
using System.Diagnostics;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH
{
    /// <summary>
    /// Ssh agent client
    /// </summary>
    public class NoAuthenticationSshClient : SshClient
    {
        #region Instances
        /// <summary>
        /// Master node name
        /// </summary>
        private readonly string _masterNodeName;

        /// <summary>
        /// Port
        /// </summary>
        private readonly int? _port;

        /// <summary>
        /// Username
        /// </summary>
        private readonly string _userName;

        /// <summary>
		///   Log4Net logger
		/// </summary>
		protected ILog _log;
        #endregion
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="masterNodeName">Master node name</param>
        /// <param name="port"></param>
        /// <param name="userName">Username</param>
        /// <exception cref="ArgumentException"></exception>
        public NoAuthenticationSshClient(string masterNodeName, int? port, string userName) : base(new ConnectionInfo(masterNodeName, userName, new PasswordAuthenticationMethod("notUsed", "notUsed")))//cannot be null
        {
            if (string.IsNullOrWhiteSpace(masterNodeName))
            {
                throw new SshClientArgumentException("NullArgument", "masterNodeName");
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new SshClientArgumentException("NullArgument", "userName");
            }

            _masterNodeName = masterNodeName;
            _port = port;
            _userName = userName;

            _log = LogManager.GetLogger(typeof(NoAuthenticationSshClient));
        }
        #endregion
        #region Methods
        /// <summary>
        /// Execute shell command
        /// </summary>
        /// <param name="commandText">Command text</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="SshCommandException"></exception>
        public SshCommandWrapper RunShellCommand(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
            {
                throw new SshClientArgumentException("NullArgument", "commandText");
            }

            if (!CheckIsAgentHasIdentities())
            {
                throw new SshCommandException("NoIdentities");
            };

            var sshCommand = new SshCommandWrapper
            {
                CommandText = commandText
            };

            using var proc = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "ssh",
                    WorkingDirectory = "/usr/bin/",
                    Arguments = $"{(_port.HasValue ? $"-p {_port.Value}" : string.Empty)} -q -o StrictHostKeyChecking=no {_userName}@{_masterNodeName} \"{commandText}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            proc.Start();
            _log.Info($"{proc.StartInfo.FileName} {proc.StartInfo.Arguments}");
            string result = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                sshCommand.ExitStatus = proc.ExitCode;
                sshCommand.Error = error;
            }

            //still can contain error from ssh
            sshCommand.Result = result;
            return sshCommand;
        }

        /// <summary>
        /// Check if Ssh agent has identities
        /// </summary>
        /// <returns></returns>
        private static bool CheckIsAgentHasIdentities()
        {
            using var proc = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "ssh-add",
                    WorkingDirectory = "/usr/bin/",
                    Arguments = $"-l",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            proc.Start();
            string result = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            return !result.Contains("The agent has no identities.") && proc.ExitCode == 0;
        }
        #endregion
    }
}
