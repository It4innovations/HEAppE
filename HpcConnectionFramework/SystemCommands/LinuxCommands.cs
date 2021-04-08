using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.MiddlewareUtils;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.DTO;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using log4net;
using Renci.SshNet;
using System;

namespace HEAppE.HpcConnectionFramework.SystemCommands
{
    /// <summary>
    /// Class: Linux system commands
    /// </summary>
    internal class LinuxCommands : ICommands
    {
        #region Properties
        /// <summary>
        /// Command
        /// </summary>
        protected CommandScriptPathDTO _commandScripts;

        /// <summary>
        /// Log4Net logger
        /// </summary>
        protected ILog _log;

        /// <summary>
        /// Interpreter command
        /// Note: In case of problems run with -lc
        /// </summary>
        public string InterpreterCommand { get { return "bash -c"; } }
        #endregion
        #region Constructors
        internal LinuxCommands()
        {
            //TODO Loading from config paths
            _log = LogManager.GetLogger(typeof(LinuxCommands));
            _commandScripts = new CommandScriptPathDTO();
        }
        #endregion
        #region Methods
        /// <summary>
        /// Method: Copy job data to temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        /// <param name="path">Path</param>
        public void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash)
        {
            string inputDirectory = $"{jobInfo.Specification.Cluster.LocalBasepath}Temp/{hash}/.";
            string outputDirectory = $"{jobInfo.Specification.Cluster.LocalBasepath}/{jobInfo.Specification.Id}";
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_commandScripts.CopyDataFromTempCmdPath} {inputDirectory} {outputDirectory}");
            _log.InfoFormat("Temp data {0} were copied to job directory {1}, result: {2}", hash, jobInfo.Specification.Id, sshCommand.Result);
        }

        /// <summary>
        /// Method: Copy job data from temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash, string path)
        {
            //if path is null or empty then all files and directories from ClusterLocalBasepath will be copied to hash directory
            string inputDirectory = $"{jobInfo.Specification.Cluster.LocalBasepath}/{jobInfo.Specification.Id}/{path}";
            inputDirectory += string.IsNullOrEmpty(path) ? "." : string.Empty;
            string outputDirectory = $"{jobInfo.Specification.Cluster.LocalBasepath}Temp/{hash}";

            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_commandScripts.CopyDataToTempCmdPath} {inputDirectory} {outputDirectory}");
            _log.InfoFormat("Job data {0}/{1} were copied to temp directory {2}, result: {3}", jobInfo.Specification.Id, path, hash, sshCommand.Result);
        }

        /// <summary>
        /// Method: Allow direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job info</param>
        public void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo)
        {
            publicKey = StringUtils.RemoveWhitespace(publicKey);
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_commandScripts.AddFiletransferKeyCmdPath} {publicKey} {jobInfo.Specification.Id}");
            _log.InfoFormat("Allow file transfer result: {0}", sshCommand.Result);
        }

        /// <summary>
        /// Method: Remove direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Conenctor</param>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job info</param>
        public void RemoveDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo)
        {
            publicKey = StringUtils.RemoveWhitespace(publicKey);
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_commandScripts.RemoveFiletransferKeyCmdPath} {publicKey}");
            _log.InfoFormat("Remove permission for direct file transfer result: {0}", sshCommand.Result);
        }

        /// <summary>
        /// Method: Create job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        public void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo)
        {
            string shellCommand = $"{_commandScripts.CreateJobDirectoryCmdPath} {jobInfo.Specification.Cluster.LocalBasepath}/{jobInfo.Specification.Id}";
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), shellCommand);
            _log.InfoFormat("Create job directory result: {0}", sshCommand.Result);
        }

        /// <summary>
        /// Method: Delete job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        public void DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo)
        {
            string shellCommand = $"rm -Rf {jobInfo.Specification.Cluster.LocalBasepath}/{jobInfo.Specification.Id}";
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), shellCommand);
            _log.InfoFormat("Job directory {0} was deleted", jobInfo.Specification.Id);
        }
        #endregion
    }
}
