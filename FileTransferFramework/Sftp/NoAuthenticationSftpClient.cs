using HEAppE.FileTransferFramework.Sftp.Commands;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System;
using System.Diagnostics;
using System.IO;

namespace HEAppE.FileTransferFramework.Sftp
{
    public class NoAuthenticationSftpClient : ExtendedSftpClient
    {
        #region Instances
        private readonly string _masterNodeName;
        private readonly string _userName;
        private readonly ILogger _logger;
        #endregion
        #region Constructors
        public NoAuthenticationSftpClient(ILogger logger, string masterNodeName, string remoteNodeTimeZone, string userName)
            : base(new ConnectionInfo(masterNodeName, userName, new PasswordAuthenticationMethod(userName, string.Empty)), remoteNodeTimeZone)
        {
            _masterNodeName = masterNodeName;
            _userName = userName;
            _logger = logger;

            CheckInputParameters();
        }
        #endregion
        #region Methods
        public TResult RunCommand<TResult>(ICommand<TResult> command)
        {
            var result = RunCommand(command.Command);
            return command.ProcessResult(_hostTimeZone, result);
        }

        public void RunCommand(ICommand command)
        {
            var result = RunCommand(command.Command);
            command.ProcessResult(_hostTimeZone, result);
        }
        #endregion
        #region Local Methods
        private SftpCommandResult RunCommand(string command)
        {
            string batchName = "/sftp/" + Guid.NewGuid().ToString();
            File.WriteAllText(batchName, command);

            if (string.IsNullOrWhiteSpace(batchName)) { throw new ArgumentException($"Argument 'commandText' cannot be null or empty"); }

            var sshCommand = new SftpCommandResult
            {
                CommandText = batchName
            };

            string output = string.Empty;
            string error = string.Empty;
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "sftp";
                proc.StartInfo.WorkingDirectory = "/usr/bin/";
                proc.StartInfo.Arguments = $"-b {batchName} {_userName}@{_masterNodeName}";
                _logger.LogInformation(proc.StartInfo.Arguments);
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.EnableRaisingEvents = true;
                proc.Start();

                output = proc.StandardOutput.ReadToEnd();
                error = proc.StandardError.ReadToEnd();

                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    sshCommand.ExitStatus = proc.ExitCode;
                    sshCommand.Error = error;
                }
            }

            sshCommand.Output = output;
            File.Delete(batchName);

            return sshCommand;
        }

        private void CheckInputParameters()
        {
            if (string.IsNullOrWhiteSpace(_masterNodeName))
            {
                throw new ArgumentException($"Argument 'masterNodeName' cannot be null or empty");
            }

            if (string.IsNullOrWhiteSpace(_userName))
            {
                throw new ArgumentException($"Argument 'userName' cannot be null or empty");
            }

            if (string.IsNullOrWhiteSpace(_hostTimeZone))
            {
                throw new ArgumentException($"Argument 'remoteNodeTimeZone' cannot be null or empty");
            }
        }
        #endregion
    }
}
