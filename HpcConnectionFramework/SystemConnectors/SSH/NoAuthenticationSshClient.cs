using log4net;
using Renci.SshNet;
using System;
using System.Diagnostics;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH
{
    public class NoAuthenticationSshClient : SshClient
    {
        #region Instances
        private readonly string _masterNodeName;
        private readonly string _userName;

        /// <summary>
		///   Log4Net logger
		/// </summary>
		protected ILog _log;
        #endregion
        #region Constructors
        public NoAuthenticationSshClient(string masterNodeName, string userName) : base(new ConnectionInfo(masterNodeName, userName, new PasswordAuthenticationMethod("notUsed", "notUsed")))//cannot be null
        {
            if (string.IsNullOrWhiteSpace(masterNodeName)) { throw new ArgumentException($"Argument 'masterNodeName' cannot be null or empty"); }
            if (string.IsNullOrWhiteSpace(userName)) { throw new ArgumentException($"Argument 'userName' cannot be null or empty"); }

            _masterNodeName = masterNodeName;
            _userName = userName;

            _log = LogManager.GetLogger(typeof(NoAuthenticationSshClient));
        }
        #endregion
        #region Methods
        public SshCommandWrapper RunShellCommand(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText)) { throw new ArgumentException($"Argument 'commandText' cannot be null or empty"); }

            if (!CheckIsAgentHasIdentities()) { throw new SshCommandException("Ssh-agent has no identities added!"); };

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
                    Arguments = $"-q -o StrictHostKeyChecking=no {_userName}@{_masterNodeName} \"{commandText}\"",
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
