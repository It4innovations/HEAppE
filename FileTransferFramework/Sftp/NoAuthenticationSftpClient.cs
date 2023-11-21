using HEAppE.Exceptions.Internal;
using HEAppE.FileTransferFramework.Sftp.Commands;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System;
using System.Diagnostics;
using System.IO;

namespace HEAppE.FileTransferFramework.Sftp
{
    public class NoAuthenticationSftpClient : SftpClient
    {
        #region Instances
        private readonly string _masterNodeName;
        private readonly string _userName;
        private readonly ILogger _logger;
        #endregion
        #region Constructors
        public NoAuthenticationSftpClient(ILogger logger, string masterNodeName, string userName)
            : base(new ConnectionInfo(masterNodeName, userName, new PasswordAuthenticationMethod(userName, string.Empty)))
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
            return command.ProcessResult(result);
        }

        public void RunCommand(ICommand command)
        {
            var result = RunCommand(command.Command);
            command.ProcessResult(result);
        }
        #endregion
        #region Local Methods
        private SftpCommandResult RunCommand(string command)
        {
            string batchName = "/sftp/" + Guid.NewGuid().ToString();
            File.WriteAllText(batchName, command);

            if (string.IsNullOrWhiteSpace(batchName)) { throw new SftpClientArgumentException("NullArgument", "commandText"); }

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
                throw new SftpClientArgumentException("NullArgument", "masterNodeName");
            }

            if (string.IsNullOrWhiteSpace(_userName))
            {
                throw new SftpClientArgumentException("NullArgument", "userName");
            }
        }
        #endregion
    }
}
