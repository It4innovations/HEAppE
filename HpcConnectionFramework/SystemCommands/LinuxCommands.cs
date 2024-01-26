﻿using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using log4net;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HEAppE.HpcConnectionFramework.SystemCommands
{
    /// <summary>
    /// Linux system commands
    /// </summary>
    internal class LinuxCommands : ICommands
    {
        #region Instances

        /// <summary>
        /// Generic command key parameter
        /// </summary>
        protected static readonly string _genericCommandKeyParameter = HPCConnectionFrameworkConfiguration.GenericCommandKeyParameter;

        #endregion
        #region Properties

        /// <summary>
        /// Command
        /// </summary>
        protected readonly CommandScriptPathConfiguration _commandScripts = HPCConnectionFrameworkConfiguration.ScriptsSettings.CommandScriptsPathSettings;

        /// <summary>
        /// Logger
        /// </summary>
        protected ILog _log;

        /// <summary>
        /// Interpreter command
        /// Note: In case of problems run with -lc
        /// </summary>
        public string InterpreterCommand
        {
            get { return "bash -c"; }
        }

        #endregion
        #region Properties
        /// <summary>
        /// Execute commnad script path
        /// </summary>
        public string ExecuteCmdScriptPath => _commandScripts.ExecuteCmdPath;

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
        /// Get generic command templates parameters from script
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="userScriptPath">Generic script path</param>
        /// <returns></returns>
        public IEnumerable<string> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath)
        {
            var genericCommandParameters = new List<string>();
            string shellCommand = $"cat {userScriptPath}";
            var sshCommand =
                SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), shellCommand);
            _log.Info($"Get parameters of script \"{userScriptPath}\", command \"{sshCommand}\"");

            foreach (Match match in Regex.Matches(sshCommand.Result,
                         @$"{_genericCommandKeyParameter}([\s\t]+[A-z_\-]+)\n",
                         RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                if (match.Success && match.Groups.Count == 2)
                {
                    genericCommandParameters.Add(match.Groups[1].Value.TrimStart());
                }
            }

            return genericCommandParameters;
        }

        /// <summary>
        /// Copy job data to temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        /// <param name="hash">Hash</param>
        public void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash)
        {
            string inputDirectory = $"{localBasePath}Temp/{hash}/.";
            string outputDirectory = $"{localBasePath}/{jobInfo.Specification.Id}";
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient),
                $"{_commandScripts.CopyDataFromTempCmdPath} {inputDirectory} {outputDirectory}");
            _log.Info(
                $"Temp data \"{hash}\" were copied to job directory \"{jobInfo.Specification.Id}\", result: \"{sshCommand.Result}\"");
        }

        /// <summary>
        /// Copy job data from temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        /// <param name="hash">Hash</param>
        public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash, string path)
        {
            //if path is null or empty then all files and directories from ClusterLocalBasepath will be copied to hash directory
            string inputDirectory = $"{localBasePath}/{jobInfo.Specification.Id}/{path}";
            inputDirectory += string.IsNullOrEmpty(path) ? "." : string.Empty;
            string outputDirectory = $"{localBasePath}Temp/{hash}";

            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient),
                $"{_commandScripts.CopyDataToTempCmdPath} {inputDirectory} {outputDirectory}");
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
            publicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey));
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient),
                $"{_commandScripts.AddFiletransferKeyCmdPath} {publicKey} {jobInfo.Specification.Id}");
            _log.InfoFormat($"Allow file transfer result: \"{sshCommand.Result.Replace("\n", string.Empty)}\"");
        }

        /// <summary>
        /// Remove direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKeys">Public keys</param>
        public void RemoveDirectFileTransferAccessForUser(object connectorClient, IEnumerable<string> publicKeys)
        {
            var cmdBuilder = new StringBuilder();
            foreach (var publicKey in publicKeys)
            {
                string base64PublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey));
                cmdBuilder.Append($"{_commandScripts.RemoveFiletransferKeyCmdPath} {base64PublicKey};");
            }

            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient),
                cmdBuilder.ToString());
            _log.Info($"Remove permission for direct file transfer result: \"{sshCommand.Result.Replace("\n", string.Empty)}\"");
        }

        /// <summary>
        /// Create job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        /// <param name="localBasePath"></param>
        /// <param name="sharedAccountsPoolMode"></param>
        public void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, bool sharedAccountsPoolMode)
        {
            localBasePath = localBasePath.TrimEnd('/');
            var cmdBuilder = new StringBuilder($"{_commandScripts.CreateJobDirectoryCmdPath} {localBasePath} {jobInfo.Specification.Id} {(sharedAccountsPoolMode ? "true" : "false")};");
            foreach (var task in jobInfo.Tasks)
            {
                var subdirectoryPath = !string.IsNullOrEmpty(task.Specification.ClusterTaskSubdirectory) 
                    ? "/{task.Specification.ClusterTaskSubdirectory}" 
                    : string.Empty;

                cmdBuilder.Append($"{_commandScripts.CreateJobDirectoryCmdPath} {localBasePath} {jobInfo.Specification.Id}/{task.Specification.Id}{subdirectoryPath}{(sharedAccountsPoolMode ? "true" : "false")};");
            }
            
            _log.Info($"Create job directory command: \"{cmdBuilder}\"");
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), cmdBuilder.ToString());
            _log.Info($"Create job directory result: \"{sshCommand.Result.Replace("\n", string.Empty)}\"");
        }

        /// <summary>
        /// Delete job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        public void DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
        {
            string shellCommand = $"rm -Rf {localBasePath}/{jobInfo.Specification.Id}";
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), shellCommand);
            _log.Info($"Job directory \"{jobInfo.Specification.Id}\" was deleted. Result: \"{sshCommand.Result}\"");
        }

        public string InitializeClusterScriptDirectory(object schedulerConnectionConnection, string clusterProjectRootDirectory, string localBasepath)
        {
            //TODO for all accounts
            var cmdBuilder = new StringBuilder();
            cmdBuilder.Append($"cd {clusterProjectRootDirectory} && ");
            
            string targetDirectory = string.Empty;
            if(!clusterProjectRootDirectory.EndsWith("HEAppE/Scripts/.key_scripts"))
            {
                targetDirectory = Path.Combine(clusterProjectRootDirectory, "HEAppE", "Scripts", ".key_scripts");
                cmdBuilder.Append($"mkdir -p {targetDirectory} && ");
            }
            
            var keyScriptsDirectoryParts = HPCConnectionFrameworkConfiguration.ScriptsSettings.KeyScriptsDirectory
                .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            cmdBuilder.Append($"rm -rf {keyScriptsDirectoryParts.FirstOrDefault()} && ");
            cmdBuilder.Append($"git clone --quiet {HPCConnectionFrameworkConfiguration.ScriptsSettings.ClusterScriptsRepository} > /dev/null && ");
            cmdBuilder.Append($"chmod +x {Path.Combine(HPCConnectionFrameworkConfiguration.ScriptsSettings.KeyScriptsDirectory, "*")} && ");
            cmdBuilder.Append($"sed -i \"s|TODO|{localBasepath.TrimEnd('/')}|g\" {Path.Combine(HPCConnectionFrameworkConfiguration.ScriptsSettings.KeyScriptsDirectory, "remote-cmd3.sh")} && ");
            
            cmdBuilder.Append($"cp -r {Path.Combine(HPCConnectionFrameworkConfiguration.ScriptsSettings.KeyScriptsDirectory, "*")} {targetDirectory} && ");
            cmdBuilder.Append($"rm -rf {keyScriptsDirectoryParts.FirstOrDefault()} && ");
            
            if (!string.IsNullOrEmpty(keyScriptsDirectoryParts.LastOrDefault()))
            {
                cmdBuilder.Append($"rm -rf {Path.Combine("~/", keyScriptsDirectoryParts.LastOrDefault())} && ");
            }

            cmdBuilder.Append($"ln -sf {targetDirectory} ~/");

            try
            {
                var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)schedulerConnectionConnection), cmdBuilder.ToString());
                _log.Info($"Initialized Cluster scripts for project");
                return sshCommand.Result;
            }
            catch (SshCommandException ex)
            {
                return ex.Message;
            }
        }
        #endregion
    }
}