using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using HEAppE.Utils;
using log4net;
using Renci.SshNet;
using System.Text;

namespace HEAppE.HpcConnectionFramework.SystemCommands
{
    /// <summary>
    /// Linux system commands
    /// </summary>
    internal class LinuxCommands : ICommands
    {
        #region Properties
        /// <summary>
        /// Command
        /// </summary>
        protected readonly CommandScriptPathConfiguration _commandScripts = HPCConnectionFrameworkConfiguration.CommandScriptsPathSettings;

        /// <summary>
        /// Logger
        /// </summary>
        protected ILog _log;

        /// <summary>
        /// Interpreter command
        /// Note: In case of problems run with -lc
        /// </summary>
        public string InterpreterCommand { get { return "bash -c"; } }
        #endregion
        #region Properties
        /// <summary>
        /// Execute commnad script path
        /// </summary>
        public string ExecutieCmdScriptPath => _commandScripts.ExecutieCmdPath;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        internal LinuxCommands()
        {
            _log = LogManager.GetLogger(typeof(LinuxCommands));
        }
        #endregion
        #region ICommands Members
        /// <summary>
        /// Copy job data to temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        /// <param name="hash">Hash</param>
        public void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash)
        {
            string inputDirectory = $"{jobInfo.Specification.Cluster.LocalBasepath}Temp/{hash}/.";
            string outputDirectory = $"{jobInfo.Specification.Cluster.LocalBasepath}/{jobInfo.Specification.Id}";
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_commandScripts.CopyDataFromTempCmdPath} {inputDirectory} {outputDirectory}");
            _log.Info($"Temp data \"{hash}\" were copied to job directory \"{jobInfo.Specification.Id}\", result: \"{sshCommand.Result}\"");
        }

        /// <summary>
        /// Copy job data from temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        /// <param name="hash">Hash</param>
        public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash, string path)
        {
            //if path is null or empty then all files and directories from ClusterLocalBasepath will be copied to hash directory
            string inputDirectory = $"{jobInfo.Specification.Cluster.LocalBasepath}/{jobInfo.Specification.Id}/{path}";
            inputDirectory += string.IsNullOrEmpty(path) ? "." : string.Empty;
            string outputDirectory = $"{jobInfo.Specification.Cluster.LocalBasepath}Temp/{hash}";

            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_commandScripts.CopyDataToTempCmdPath} {inputDirectory} {outputDirectory}");
            _log.Info($"Job data \"{jobInfo.Specification.Id}/{path}\" were copied to temp directory \"{hash}\", result: \"{sshCommand.Result}\"");
        }

        /// <summary>
        /// Allow direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job information</param>
        public void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo)
        {
            publicKey = StringUtils.RemoveWhitespace(publicKey);
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_commandScripts.AddFiletransferKeyCmdPath} {publicKey} {jobInfo.Specification.Id}");
            _log.InfoFormat($"Allow file transfer result: \"{sshCommand.Result}\"");
        }

        /// <summary>
        /// Remove direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKey">Public key</param>
        public void RemoveDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey)
        {
            publicKey = StringUtils.RemoveWhitespace(publicKey);
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_commandScripts.RemoveFiletransferKeyCmdPath} {publicKey}");
            _log.Info($"Remove permission for direct file transfer result: \"{sshCommand.Result}\"");
        }

        /// <summary>
        /// Create job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        public void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo)
        {
            var cmdBuilder = new StringBuilder($"{_commandScripts.CreateJobDirectoryCmdPath} {jobInfo.Specification.Cluster.LocalBasepath}/{jobInfo.Specification.Id};");
            foreach (var task in jobInfo.Tasks)
            {
                var path = !string.IsNullOrEmpty(task.Specification.ClusterTaskSubdirectory)
                    ? $"{_commandScripts.CreateJobDirectoryCmdPath} {jobInfo.Specification.Cluster.LocalBasepath}/{jobInfo.Specification.Id}/{task.Specification.Id}/{task.Specification.ClusterTaskSubdirectory};"
                    : $"{_commandScripts.CreateJobDirectoryCmdPath} {jobInfo.Specification.Cluster.LocalBasepath}/{jobInfo.Specification.Id}/{task.Specification.Id};";

                cmdBuilder.Append(path);
            }
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), cmdBuilder.ToString());
            _log.Info($"Create job directory result: \"{sshCommand.Result.Replace("\n", string.Empty)}\"");
        }

        /// <summary>
        /// Delete job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        public void DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo)
        {
            string shellCommand = $"rm -Rf {jobInfo.Specification.Cluster.LocalBasepath}/{jobInfo.Specification.Id}";
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), shellCommand);
            _log.Info($"Job directory \"{jobInfo.Specification.Id}\" was deleted. Result: \"{sshCommand.Result}\"");
        }
        #endregion
    }
}
