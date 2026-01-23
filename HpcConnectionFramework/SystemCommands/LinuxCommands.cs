using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using log4net;
using Renci.SshNet;

namespace HEAppE.HpcConnectionFramework.SystemCommands;

/// <summary>
///     Linux system commands
/// </summary>
internal class LinuxCommands : ICommands
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    internal LinuxCommands()
    {
        _log = LogManager.GetLogger(typeof(LinuxCommands));
    }

    #endregion

    #region Instances

    /// <summary>
    ///     Generic command key parameter
    /// </summary>
    protected static readonly string _genericCommandKeyParameter =
        HPCConnectionFrameworkConfiguration.GenericCommandKeyParameter;

    /// <summary>
    ///     Command
    /// </summary>
    protected readonly CommandScriptPathConfiguration _commandScripts =
        HPCConnectionFrameworkConfiguration.ScriptsSettings.CommandScriptsPathSettings;

    /// <summary>
    ///     Script Configuration
    /// </summary>
    protected readonly ScriptsConfiguration _scripts = HPCConnectionFrameworkConfiguration.ScriptsSettings;

    /// <summary>
    ///     Logger
    /// </summary>
    protected ILog _log;

    /// <summary>
    ///     Interpreter command
    ///     Note: In case of problems run with -lc
    /// </summary>
    public string InterpreterCommand => "bash -c";

    #endregion

    #region ICommands Members

    /// <summary>
    ///     Get generic command templates parameters from script
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="userScriptPath">Generic script path</param>
    /// <returns></returns>
    public IEnumerable<string> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath)
    {
        var genericCommandParameters = new List<string>();
        var shellCommand = $"cat {userScriptPath}";
        var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), shellCommand);
        _log.Info($"Get parameters of script \"{userScriptPath}\", command \"{sshCommand}\"");

        foreach (Match match in Regex.Matches(sshCommand.Result,
                     @$"{_genericCommandKeyParameter}([\s\t]+[A-z_\-]+)\n",
                     RegexOptions.IgnoreCase | RegexOptions.Compiled))
            if (match.Success && match.Groups.Count == 2)
                genericCommandParameters.Add(match.Groups[1].Value.TrimStart());

        return genericCommandParameters;
    }

    /// <summary>
    ///     Copy job data to temp folder
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job information</param>
    /// <param name="hash">Hash</param>
    public void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash)
    {
        string account = jobInfo.Specification.ClusterUser.Username;
        var inputDirectory = $"{localBasePath}/{_scripts.InstanceIdentifierPath}/{_scripts.SubExecutionsPath}/{account}Temp/{hash}/.";
        var outputDirectory = $"{localBasePath}/{_scripts.InstanceIdentifierPath}/{_scripts.SubExecutionsPath}/{account}/{jobInfo.Specification.Id}";
        var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient),
            $"{HPCConnectionFrameworkConfiguration.GetPathToScript(jobInfo.Project.AccountingString, _commandScripts.CopyDataFromTempCmdScriptName)} {inputDirectory} {outputDirectory}");
        _log.Info(
            $"Temp data \"{hash}\" were copied to job directory \"{jobInfo.Specification.Id}\", result: \"{sshCommand.Result}\"");
    }

    /// <summary>
    ///     Copy job data from temp folder
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job information</param>
    /// <param name="hash">Hash</param>
    public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash,
        string path)
    {
        string account = jobInfo.Specification.ClusterUser.Username;
        //if path is null or empty then all files and directories from ClusterLocalBasepath will be copied to hash directory
        var inputDirectory = $"{localBasePath}/{_scripts.InstanceIdentifierPath}/{_scripts.SubExecutionsPath}/{account}/{jobInfo.Specification.Id}/{path}";
        inputDirectory += string.IsNullOrEmpty(path) ? "." : string.Empty;
        var outputDirectory = $"{localBasePath}/{_scripts.InstanceIdentifierPath}/{_scripts.SubExecutionsPath}/{account}Temp/{hash}";

        var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient),
            $"{HPCConnectionFrameworkConfiguration.GetPathToScript(jobInfo.Project.AccountingString, _commandScripts.CopyDataToTempCmdScriptName)} {inputDirectory} {outputDirectory}");
        _log.Info(
            $"Job data \"{jobInfo.Specification.Id}/{path}\" were copied to temp directory \"{hash}\", result: \"{sshCommand.Result}\"");
    }

    /// <summary>
    ///     Allow direct file transfer access for user
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="publicKey">Public key</param>
    /// <param name="jobInfo">Job information</param>
    public void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey,
        SubmittedJobInfo jobInfo)
    {
        publicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey));
        string remoteCmd3Path = HPCConnectionFrameworkConfiguration.GetPathToScript(jobInfo.Project.AccountingString, "remote-cmd3.sh");
        var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient),
            $"{HPCConnectionFrameworkConfiguration.GetPathToScript(jobInfo.Project.AccountingString, _commandScripts.AddFiletransferKeyCmdScriptName)} {publicKey} {jobInfo.Specification.Id} {remoteCmd3Path}");
        _log.InfoFormat($"Allow file transfer result: \"{sshCommand.Result.Replace("\n", string.Empty)}\"");
    }

    /// <summary>
    ///     Remove direct file transfer access for user
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="publicKeys">Public keys</param>
    public void RemoveDirectFileTransferAccessForUser(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString)
    {
        SshCommandWrapper sshCommand;
        var adapter = new SshClientAdapter((SshClient)connectorClient);
        var cmdBuilder = new StringBuilder();
        foreach (var publicKey in publicKeys)
        {
            var base64PublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey));
            var cmdText = $"{HPCConnectionFrameworkConfiguration.GetPathToScript(projectAccountingString, _commandScripts.RemoveFiletransferKeyCmdScriptName)} {base64PublicKey};";

            if (cmdBuilder.Length + cmdText.Length > 55000)
            {
                sshCommand = SshCommandUtils.RunSshCommand(adapter, cmdBuilder.ToString());
                _log.Info(
                    $"Remove permission for direct file transfer result: \"{sshCommand.Result.Replace("\n", string.Empty)}\"");
                cmdBuilder.Clear();
            }

            cmdBuilder.Append(cmdText);
        }

        if (cmdBuilder.Length > 0)
        {
            sshCommand = SshCommandUtils.RunSshCommand(adapter, cmdBuilder.ToString());
            _log.Info(
                $"Remove permission for direct file transfer result: \"{sshCommand.Result.Replace("\n", string.Empty)}\"");
        }
    }


    /// <summary>
    ///     Create job directory
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job information</param>
    /// <param name="localBasePath"></param>
    /// <param name="sharedAccountsPoolMode"></param>
    public void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode)
    {
        string account = jobInfo.Specification.ClusterUser.Username;

        localBasePath = localBasePath.TrimEnd('/');
        var cmdBuilder =
            new StringBuilder(
                $"{HPCConnectionFrameworkConfiguration.GetPathToScript(jobInfo.Project.AccountingString, _commandScripts.CreateJobDirectoryCmdScriptName)} {localBasePath} {_scripts.InstanceIdentifierPath}/{_scripts.SubExecutionsPath} {account}/{jobInfo.Specification.Id} {(sharedAccountsPoolMode ? "true" : "false")};");
        foreach (var task in jobInfo.Tasks)
        {
            var subdirectoryPath = !string.IsNullOrEmpty(task.Specification.ClusterTaskSubdirectory)
                ? $"/{task.Specification.ClusterTaskSubdirectory}"
                : string.Empty;

            cmdBuilder.Append(
                $"{HPCConnectionFrameworkConfiguration.GetPathToScript(jobInfo.Project.AccountingString, _commandScripts.CreateJobDirectoryCmdScriptName)} {localBasePath} {_scripts.InstanceIdentifierPath}/{_scripts.SubExecutionsPath} {account}/{jobInfo.Specification.Id}/{task.Specification.Id}{subdirectoryPath} {(sharedAccountsPoolMode ? "true" : "false")};");
        }

        _log.Info($"Create job directory command: \"{cmdBuilder}\"");
        
        var sshCommand =
            SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), cmdBuilder.ToString());
        _log.Info($"Create job directory result: \"{sshCommand.Result.Replace("\n", string.Empty)}\"");
    }

    /// <summary>
    ///     Delete job directory
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job information</param>
    public bool DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
    {
        var shellCommand = $"rm -Rf {localBasePath}/{_scripts.InstanceIdentifierPath}/{_scripts.SubExecutionsPath}/{jobInfo.Specification.ClusterUser.Username}/{jobInfo.Specification.Id}";
        try
        {
            var sshCommand =
                SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), shellCommand);
            _log.Info($"Job directory \"{jobInfo.Specification.Id}\" was deleted. Result: \"{sshCommand.Result}\"");
            return true;
        }
        catch (SshCommandException ex)
        {
            _log.Error($"Job directory \"{jobInfo.Specification.Id}\" was not deleted. Error: \"{ex.Message}\"");
            return false;
        }
    }

    /// <summary>
    ///     Initialize Cluster Script Directory
    /// </summary>
    /// <param name="schedulerConnectionConnection">Connector</param>
    /// <param name="clusterProjectRootDirectory">Cluster project root path</param>
    /// <param name="overwriteExistingProjectRootDirectory">Overwrite existin project root directory</param>
    /// <param name="localBasepath">Cluster execution path</param>
    /// <param name="isServiceAccount">Is servis account</param>
    /// <param name="account">Cluster username</param>
   public bool InitializeClusterScriptDirectory(object schedulerConnectionConnection,
     string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath, string account, bool isServiceAccount)
     {
         if (isServiceAccount)
             return true;
         
         var rootDir = Path.Combine(_scripts.ScriptsBasePath, $".{clusterProjectRootDirectory}").Replace('\\', '/');
         
         string bashSafeRootDir = rootDir.StartsWith("~/") 
             ? "~/" + "\"" + rootDir.Substring(2) + "\"" 
             : "\"" + rootDir + "\"";

         var keyScriptsDir = rootDir.EndsWith("/") ? rootDir + ".key_scripts" : rootDir + "/.key_scripts";
         string bashSafeKeyScriptsDir = bashSafeRootDir.EndsWith("\"") 
             ? bashSafeRootDir.Insert(bashSafeRootDir.Length - 1, "/.key_scripts") 
             : bashSafeRootDir + "/.key_scripts";

         // Persistent cache directory for git repo to allow version comparison
         var repoCacheDir = rootDir.EndsWith("/") ? rootDir + ".repo_cache" : rootDir + "/.repo_cache";
         string bashSafeRepoCacheDir = bashSafeRootDir.EndsWith("\"") 
             ? bashSafeRootDir.Insert(bashSafeRootDir.Length - 1, "/.repo_cache") 
             : bashSafeRootDir + "/.repo_cache";

         var repoUrl = HPCConnectionFrameworkConfiguration.ScriptsSettings.ClusterScriptsRepository;
         var branch = HPCConnectionFrameworkConfiguration.ScriptsSettings.ClusterScriptsRepositoryBranch;
         var rawRepoDir = HPCConnectionFrameworkConfiguration.ScriptsSettings.KeyScriptsDirectoryInRepository.Replace('\\', '/').TrimEnd('/');
         
         var cmdBuilder = new StringBuilder();
         
         // Ensure root directory exists
         cmdBuilder.Append($@"mkdir -p {bashSafeRootDir} && ");

         // Git logic: Clone if missing, Fetch & Check if exists
         cmdBuilder.Append($@"
            UPDATE_NEEDED=0;
            if [ ! -d {bashSafeRepoCacheDir}/.git ]; then
                rm -rf {bashSafeRepoCacheDir};
                git clone --single-branch -b {branch} --quiet {repoUrl} {bashSafeRepoCacheDir} 2>&1;
                if [ $? -ne 0 ]; then echo ""GIT_ERROR""; exit 1; fi;
                UPDATE_NEEDED=1;
            else
                cd {bashSafeRepoCacheDir};
                git fetch origin {branch} --quiet;
                LOCAL_HASH=$(git rev-parse HEAD);
                REMOTE_HASH=$(git rev-parse origin/{branch});
                
                if [ ""$LOCAL_HASH"" != ""$REMOTE_HASH"" ]; then
                    git reset --hard origin/{branch} --quiet;
                    UPDATE_NEEDED=1;
                fi;
                cd - > /dev/null;
            fi;
         ");

         if (overwriteExistingProjectRootDirectory)
         {
             cmdBuilder.Append("UPDATE_NEEDED=1; ");
         }

         // If the target directory was deleted manually, force update
         cmdBuilder.Append($@"
            if [ ! -d {bashSafeKeyScriptsDir} ]; then
                UPDATE_NEEDED=1;
            fi;
         ");

         var sedReplacement = $"{localBasepath}/{_scripts.InstanceIdentifierPath}/{_scripts.SubExecutionsPath}/{account}";

         // Apply changes if needed
         cmdBuilder.Append($@"
            if [ ""$UPDATE_NEEDED"" -eq 1 ]; then
                rm -rf {bashSafeKeyScriptsDir} &&
                cp -r {bashSafeRepoCacheDir}/{rawRepoDir} {bashSafeKeyScriptsDir} &&
                chmod -R 755 {bashSafeKeyScriptsDir} &&
                sed -i ""s|TODO|{sedReplacement}|g"" {bashSafeKeyScriptsDir}/remote-cmd3.sh &&
                echo ""INSTALLED_UPDATED"";
            else
                echo ""SKIPPED_UP_TO_DATE"";
            fi
         ");

         try
         {
             var sshCommand = SshCommandUtils.RunSshCommand(
                 new SshClientAdapter((SshClient)schedulerConnectionConnection), 
                 cmdBuilder.ToString()
             );

             var result = sshCommand.Result.Trim();
             
             if (sshCommand.ExitStatus != 0)
             {
                 if (result.Contains("GIT_ERROR"))
                     _log.Error($"Cluster scripts initialization failed: Git clone/fetch error.");
                 else
                     _log.Error($"Cluster scripts initialization failed (ExitCode: {sshCommand.ExitStatus}): {result}");
                 
                 return false;
             }

             if (result.Contains("SKIPPED_UP_TO_DATE"))
             {
                 _log.Info($"Skipping initialization for '{clusterProjectRootDirectory}', scripts are up to date.");
                 return true;
             }

             if (result.Contains("INSTALLED_UPDATED"))
             {
                 _log.Info($"Cluster scripts initialized/updated successfully.");
                 return true;
             }

             return true;
         }
         catch (Exception ex)
         {
             _log.Error($"Cluster scripts initialization exception: \"{ex.Message}\"");
             return false;
         }
     }

    public bool CopyJobFiles(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations)
    {
        _log.Info($"Copying job files to cluster");
        var cmdBuilder = new StringBuilder();
        foreach (var sourceDestination in sourceDestinations)
        {
            //mkdir dest, eremove filename
            string destinationDirectory = Path.GetDirectoryName(sourceDestination.Item2);
            cmdBuilder.Append($"mkdir -p {destinationDirectory};");
            cmdBuilder.Append($"[ -f {sourceDestination.Item1} ] && cp {sourceDestination.Item1} {sourceDestination.Item2};");
        }

        try
        {
            _log.Info($"Copy job files command: \"{cmdBuilder}\"");
            var sshCommand =
                SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)schedulerConnectionConnection),
                    cmdBuilder.ToString());
            _log.Info($"Copy job files result: \"{sshCommand.Result}\"");
        }
        catch (SshCommandException ex)
        {
            _log.Error($"Copy job files failed: \"{ex.Message}\"");
            return false;
        }

        return true;
    }

    #endregion
}