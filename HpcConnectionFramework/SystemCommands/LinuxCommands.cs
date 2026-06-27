using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using Microsoft.Extensions.Logging;
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
    internal LinuxCommands(ILogger logger)
    {
        _logger = logger;
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
    protected ILogger _logger;

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
    public async Task<IEnumerable<string>> GetParametersFromGenericUserScriptAsync(object connectorClient, string userScriptPath)
    {
        var genericCommandParameters = new List<string>();
        var shellCommand = $"cat {userScriptPath}";
        var sshCommand = await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)connectorClient), shellCommand, _logger);
        _logger.LogInformation($"Get parameters of script \"{userScriptPath}\", command \"{sshCommand}\"");

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
    public async Task CopyJobDataFromTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash)
    {
        string account = jobInfo.Specification.ClusterUser.Username;
        localBasePath = ExpandPath(localBasePath, jobInfo, account);
        var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        var inputDirectory = $"{localBasePath}/{clusterConfig.InstanceIdentifierPath}/{clusterConfig.SubExecutionsPath}/{account}Temp/{hash}/.";
        var outputDirectory = $"{localBasePath}/{clusterConfig.InstanceIdentifierPath}/{clusterConfig.SubExecutionsPath}/{account}/{jobInfo.Specification.Id}";
        var sshCommand = await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)connectorClient),
            $"{clusterConfig.GetPathToScript(jobInfo.Project.AccountingString, _commandScripts.CopyDataFromTempCmdScriptName)} {inputDirectory} {outputDirectory}",
            _logger);
        _logger.LogInformation(
            $"Temp data \"{hash}\" were copied to job directory \"{jobInfo.Specification.Id}\", result: \"{sshCommand.Result}\"");
    }

    /// <summary>
    ///     Copy job data from temp folder
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job information</param>
    /// <param name="hash">Hash</param>
    public async Task CopyJobDataToTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash,
        string path)
    {
        string account = jobInfo.Specification.ClusterUser.Username;
        localBasePath = ExpandPath(localBasePath, jobInfo, account);
        var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        //if path is null or empty then all files and directories from ClusterLocalBasepath will be copied to hash directory
        var inputDirectory = $"{localBasePath}/{clusterConfig.InstanceIdentifierPath}/{clusterConfig.SubExecutionsPath}/{account}/{jobInfo.Specification.Id}/{path}";
        inputDirectory += string.IsNullOrEmpty(path) ? "." : string.Empty;
        var outputDirectory = $"{localBasePath}/{clusterConfig.InstanceIdentifierPath}/{clusterConfig.SubExecutionsPath}/{account}Temp/{hash}";

        var sshCommand = await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)connectorClient),
            $"{clusterConfig.GetPathToScript(jobInfo.Project.AccountingString, _commandScripts.CopyDataToTempCmdScriptName)} {inputDirectory} {outputDirectory}",
            _logger);
        _logger.LogInformation(
            $"Job data \"{jobInfo.Specification.Id}/{path}\" were copied to temp directory \"{hash}\", result: \"{sshCommand.Result}\"");
    }

    /// <summary>
    ///     Allow direct file transfer access for user
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="publicKey">Public key</param>
    /// <param name="jobInfo">Job information</param>
    public async Task AllowDirectFileTransferAccessForUserToJobAsync(object connectorClient, string publicKey,
        SubmittedJobInfo jobInfo)
    {
        publicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey));
        var clusterConfig3 = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        string remoteCmd3Path = clusterConfig3.GetPathToScript(jobInfo.Project.AccountingString, "remote-cmd3.sh");
        var sshCommand = await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)connectorClient),
            $"{clusterConfig3.GetPathToScript(jobInfo.Project.AccountingString, _commandScripts.AddFiletransferKeyCmdScriptName)} {publicKey} {jobInfo.Specification.Id} {remoteCmd3Path}",
            _logger);
        _logger.LogInformation($"Allow file transfer result: \"{sshCommand.Result.Replace("\n", string.Empty)}\"");
    }

    /// <summary>
    ///     Remove direct file transfer access for user
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="publicKeys">Public keys</param>
    public async Task RemoveDirectFileTransferAccessForUserAsync(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString)
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
                sshCommand = await SshCommandUtils.RunSshCommandAsync(adapter, cmdBuilder.ToString(), _logger);
                _logger.LogInformation(
                    $"Remove permission for direct file transfer result: \"{sshCommand.Result.Replace("\n", string.Empty)}\"");
                cmdBuilder.Clear();
            }

            cmdBuilder.Append(cmdText);
        }

        if (cmdBuilder.Length > 0)
        {
            sshCommand = await SshCommandUtils.RunSshCommandAsync(adapter, cmdBuilder.ToString(), _logger);
            _logger.LogInformation(
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
    public async Task CreateJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode)
    {
        string account = jobInfo.Specification.ClusterUser.Username;
        localBasePath = ExpandPath(localBasePath, jobInfo, account);

        localBasePath = localBasePath.TrimEnd('/');
        var clusterConfig4 = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        var cmdBuilder =
            new StringBuilder(
                $"{clusterConfig4.GetPathToScript(jobInfo.Project.AccountingString, _commandScripts.CreateJobDirectoryCmdScriptName)} {localBasePath} {clusterConfig4.InstanceIdentifierPath}/{clusterConfig4.SubExecutionsPath} {account}/{jobInfo.Specification.Id} {(sharedAccountsPoolMode ? "true" : "false")};");
        foreach (var task in jobInfo.Tasks)
        {
            var subdirectoryPath = !string.IsNullOrEmpty(task.Specification.ClusterTaskSubdirectory)
                ? $"/{task.Specification.ClusterTaskSubdirectory}"
                : string.Empty;

            cmdBuilder.Append(
                $"{clusterConfig4.GetPathToScript(jobInfo.Project.AccountingString, _commandScripts.CreateJobDirectoryCmdScriptName)} {localBasePath} {clusterConfig4.InstanceIdentifierPath}/{clusterConfig4.SubExecutionsPath} {account}/{jobInfo.Specification.Id}/{task.Specification.Id}{subdirectoryPath} {(sharedAccountsPoolMode ? "true" : "false")};");
        }

        _logger.LogInformation($"Create job directory command: \"{cmdBuilder}\"");
        
        var sshCommand =
            await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)connectorClient), cmdBuilder.ToString(), _logger);
        _logger.LogInformation($"Create job directory result: \"{sshCommand.Result.Replace("\n", string.Empty)}\"");
    }

    /// <summary>
    ///     Delete job directory
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job information</param>
    public async Task<bool> DeleteJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
    {
        string account = jobInfo.Specification.ClusterUser.Username;
        localBasePath = ExpandPath(localBasePath, jobInfo, account);
        var shellCommand = $"rm -Rf {localBasePath}/{_scripts.InstanceIdentifierPath}/{_scripts.SubExecutionsPath}/{account}/{jobInfo.Specification.Id}";
        try
        {
            var sshCommand =
                await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)connectorClient), shellCommand, _logger);
            _logger.LogInformation($"Job directory \"{jobInfo.Specification.Id}\" was deleted. Result: \"{sshCommand.Result}\"");
            return true;
        }
        catch (SshCommandException ex)
        {
            _logger.LogError($"Job directory \"{jobInfo.Specification.Id}\" was not deleted. Error: \"{ex.Message}\"");
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
    public async Task<bool> InitializeClusterScriptDirectoryAsync(object schedulerConnectionConnection,
        string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath, string account, bool isServiceAccount, Dictionary<string, string>? customConfiguration)
    {
        if (isServiceAccount) return true;

        localBasepath = ExpandPathSimple(localBasepath, account);

        var clusterConfig = ClusterRuntimeConfiguration.For(customConfiguration);
        var rootDir = Path.Combine(clusterConfig.ScriptsBasePath, $".{clusterProjectRootDirectory}").Replace('\\', '/');
        string bashSafeRootDir = rootDir.StartsWith("~/") 
            ? "~/" + "\"" + rootDir.Substring(2) + "\"" 
            : "\"" + rootDir + "\"";
        
        var repoUrl = HPCConnectionFrameworkConfiguration.ScriptsSettings.ClusterScriptsRepository;
        var branch = HPCConnectionFrameworkConfiguration.ScriptsSettings.ClusterScriptsRepositoryBranch;
        var sedReplacement = $"{localBasepath}/{clusterConfig.InstanceIdentifierPath}/{clusterConfig.SubExecutionsPath}/{account}";

        var cmdBuilder = new StringBuilder();
        cmdBuilder.Append($@"mkdir -p {bashSafeRootDir} && cd {bashSafeRootDir} && ");
        cmdBuilder.Append($@"
            UPDATE_NEEDED=0;
            REPO_DIR="".repo_cache"";
            if [ ! -d ""$REPO_DIR""/.git ]; then
                rm -rf ""$REPO_DIR"";
                git clone --single-branch -b {branch} --quiet {repoUrl} ""$REPO_DIR"" 2>&1 || {{ echo ""GIT_ERROR""; exit 1; }};
                UPDATE_NEEDED=1;
            else
                cd ""$REPO_DIR"" && 
                LOCAL_HASH=$(git rev-parse HEAD) &&
                git pull origin {branch} --quiet && 
                NEW_HASH=$(git rev-parse HEAD);
                if [ ""$LOCAL_HASH"" != ""$NEW_HASH"" ]; then
                    UPDATE_NEEDED=1;
                fi;
                cd - > /dev/null;
            fi;
            if [ ! -d "".key_scripts"" ]; then UPDATE_NEEDED=1; fi;
            if [ ""$UPDATE_NEEDED"" -eq 1 ]; then
                SOURCE_PATH=$(find ""$REPO_DIR""/HPC -maxdepth 1 -type d -name "".key_scripts"" 2>/dev/null | head -n 1);
                if [ -z ""$SOURCE_PATH"" ]; then
                    SOURCE_PATH=$(find ""$REPO_DIR"" -type d -name "".key_scripts"" | head -n 1);
                fi;
                if [ -z ""$SOURCE_PATH"" ]; then echo ""ERROR: .key_scripts not found""; exit 1; fi;
                mkdir -p .key_scripts &&
                cp -rf ""$SOURCE_PATH""/* .key_scripts/ &&
                chmod -R 755 .key_scripts &&
                sed -i ""s|TODO|{sedReplacement}|g"" .key_scripts/remote-cmd3.sh &&
                echo ""INSTALLED_UPDATED"";
            else
                echo ""SKIPPED_UP_TO_DATE"";
            fi");

        try
        {
            var sshCommand = await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)schedulerConnectionConnection), cmdBuilder.ToString(), _logger);
            if (sshCommand.ExitStatus != 0)
            {
                _logger.LogError($"Initialization failed: {sshCommand.Result}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CopyJobFilesAsync(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, bool sharedAccountsPoolMode)
    {
        _logger.LogInformation($"Copying job files to cluster");
        var cmdBuilder = new StringBuilder();

        var clusterProject = jobInfo.Specification.Cluster.ClusterProjects
            .FirstOrDefault(cp => cp.ProjectId == jobInfo.Specification.ProjectId);

        if (clusterProject != null)
        {
            var projectBasePath = string.IsNullOrEmpty(clusterProject.ProjectStoragePath) 
                ? clusterProject.ScratchStoragePath 
                : clusterProject.ProjectStoragePath;
            
            string account = jobInfo.Specification.ClusterUser.Username;
            projectBasePath = ExpandPath(projectBasePath, jobInfo, account);

            var heappeJobsDir = $"{_scripts.InstanceIdentifierPath.TrimStart('/')}/{_scripts.JobLogArchiveSubPath.TrimStart('/')}";
            var copyFilesClusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);

            cmdBuilder.Append(
                $"{copyFilesClusterConfig.GetPathToScript(jobInfo.Project.AccountingString, _commandScripts.CreateJobDirectoryCmdScriptName)} {projectBasePath.TrimEnd('/')} {heappeJobsDir} {account}/{jobInfo.Specification.Id} {(sharedAccountsPoolMode ? "true" : "false")};");

            foreach (var task in jobInfo.Tasks)
            {
                var subdirectoryPath = !string.IsNullOrEmpty(task.Specification.ClusterTaskSubdirectory)
                    ? $"/{task.Specification.ClusterTaskSubdirectory}"
                    : string.Empty;

                cmdBuilder.Append(
                    $"{copyFilesClusterConfig.GetPathToScript(jobInfo.Project.AccountingString, _commandScripts.CreateJobDirectoryCmdScriptName)} {projectBasePath.TrimEnd('/')} {heappeJobsDir} {account}/{jobInfo.Specification.Id}/{task.Specification.Id}{subdirectoryPath} {(sharedAccountsPoolMode ? "true" : "false")};");
            }
        }

        foreach (var sourceDestination in sourceDestinations)
        {
            cmdBuilder.Append($"[ -f {sourceDestination.Item1} ] && cp {sourceDestination.Item1} {sourceDestination.Item2};");
        }

        try
        {
            _logger.LogInformation($"Copy job files command: \"{cmdBuilder}\"");
            var sshCommand =
                await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)schedulerConnectionConnection),
                    cmdBuilder.ToString(), _logger);
            _logger.LogInformation($"Copy job files result: \"{sshCommand.Result}\"");
        }
        catch (SshCommandException ex)
        {
            _logger.LogError($"Copy job files failed: \"{ex.Message}\"");
            return false;
        }

        return true;
    }

    private static string ExpandPathSimple(string path, string username)
    {
        if (string.IsNullOrEmpty(path)) return path;
        return path.Replace("$USER", username).Replace("${USER}", username);
    }

    private string GetHomeDirectory(SubmittedJobInfo jobInfo, string clusterUser)
    {
        var homeDirTemplate = "/users/{username}";
        if (jobInfo.Specification.Cluster?.CustomConfiguration != null &&
            jobInfo.Specification.Cluster.CustomConfiguration.TryGetValue("HomeDirectoryTemplate", out var template))
        {
            homeDirTemplate = template;
        }

        return homeDirTemplate
            .Replace("{username}", clusterUser)
            .Replace("{USER}", clusterUser)
            .Replace("$USER", clusterUser);
    }

    private string ExpandPath(string path, SubmittedJobInfo jobInfo, string username)
    {
        if (string.IsNullOrEmpty(path)) return path;
        var expanded = path.Replace("$USER", username).Replace("${USER}", username);
        if (expanded.Contains("$HOME"))
        {
            var homeDir = GetHomeDirectory(jobInfo, username);
            expanded = expanded.Replace("$HOME", homeDir);
        }
        return expanded;
    }

    #endregion
}