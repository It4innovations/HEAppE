using HEAppE.Exceptions.Internal;
using HEAppE.FileTransferFramework.Sftp.Commands;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace HEAppE.FileTransferFramework.Sftp
{
    public class NoAuthenticationSftpClient : SftpClient
    {
        #region Instances
        private readonly string _masterNodeName;
        private readonly string _userName;
        private readonly int _port;
        private readonly ILogger _logger;
        #endregion
        #region Constructors
        public NoAuthenticationSftpClient(ILogger logger, string masterNodeName, string userName, int? port)
            : base(new ConnectionInfo(masterNodeName, port ?? 22, userName, new PasswordAuthenticationMethod(userName, string.Empty)))
        {
            _masterNodeName = masterNodeName;
            _userName = userName;
            _port = port ?? 22;
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
            var sshCommand = new SftpCommandResult
            {
                CommandText = command
            };

            string output = string.Empty;
            string error = string.Empty;
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "sftp";
                proc.StartInfo.WorkingDirectory = "/usr/bin/";
                proc.StartInfo.Arguments = $"-P {SanitizePort(_port)} -q -o StrictHostKeyChecking=no {SanitizeUserName(_userName)}@{SanitizeNodeName(_masterNodeName)}";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.EnableRaisingEvents = true;

                _logger.LogInformation(proc.StartInfo.Arguments);
                proc.Start();
                proc.StandardInput.WriteLine(command);
                proc.StandardInput.WriteLine("quit");
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
        #region SanitizeMethods
        private string SanitizePort(int port)
        {
            // Allow only numbers
            return Regex.Replace(port.ToString(), @"[^0-9]", "");
        }

        private string SanitizeUserName(string username)
        {
            // Allow only alphanumeric characters
            return Regex.Replace(username, @"[^a-zA-Z0-9]", "");
        }

        private string SanitizeNodeName(string nodeName)
        {
            // Allow only alphanumeric and specific safe characters
            return Regex.Replace(nodeName, @"[^a-zA-Z0-9.-]", "");
        }

        #endregion
    }
}
