using log4net;
using Renci.SshNet;
using System;
using System.Diagnostics;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH
{
    public class NoAuthenticationSshClient : SshClient
    {
        private readonly string _masterNodeName;
        private readonly string _userName;

        /// <summary>
		///   Log4Net logger
		/// </summary>
		protected ILog _log;

        public NoAuthenticationSshClient(string masterNodeName, string userName) : base(new ConnectionInfo(masterNodeName, userName, new PasswordAuthenticationMethod("notUsed", "notUsed")))//cannot be null
        {
            if (string.IsNullOrWhiteSpace(masterNodeName)) { throw new ArgumentException($"Argument 'masterNodeName' cannot be null or empty"); }
            if (string.IsNullOrWhiteSpace(userName)) { throw new ArgumentException($"Argument 'userName' cannot be null or empty"); }

            _masterNodeName = masterNodeName;
            _userName = userName;

            _log = LogManager.GetLogger(typeof(NoAuthenticationSshClient));
        }

        public SshCommandWrapper RunShellCommand(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText)) { throw new ArgumentException($"Argument 'commandText' cannot be null or empty"); }

            var sshCommand = new SshCommandWrapper
            {
                CommandText = commandText
            };

            string result = string.Empty;
            string error = string.Empty;

            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "ssh";
                proc.StartInfo.WorkingDirectory = "/usr/bin/";
                proc.StartInfo.Arguments = $"-q -o StrictHostKeyChecking=no {_userName}@{_masterNodeName} \"{commandText}\"";
                _log.Info($"{proc.StartInfo.FileName} {proc.StartInfo.Arguments}");
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.EnableRaisingEvents = true;
                proc.Start();
                
                result = proc.StandardOutput.ReadToEnd();
                error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    sshCommand.ExitStatus = proc.ExitCode;
                    sshCommand.Error = error;
                }
            }

            sshCommand.Result = result; //still can contain error from ssh
            return sshCommand;
        }
    }
}
