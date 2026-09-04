using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        var shellCommand = $"rm -Rf {localBasePath}/{clusterConfig.InstanceIdentifierPath}/{clusterConfig.SubExecutionsPath}/{account}/{jobInfo.Specification.Id}";
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

        // 1. Run the custom command prefix first (e.g. to create bypass files like .avoid_load_def_modules.mn5)
        var prefix = clusterConfig.SshCommandPrefix;
        if (!string.IsNullOrEmpty(prefix))
        {
            try
            {
                _logger.LogInformation($"Running custom SSH command prefix as pre-step: {prefix}");
                using var cmd = ((Renci.SshNet.SshClient)schedulerConnectionConnection).CreateCommand(prefix);
                await Task.Factory.FromAsync(cmd.BeginExecute(), cmd.EndExecute);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"SSH command prefix pre-step finished: {ex.Message}");
            }
        }

        // 2. Proceed with main script directory setup
        var rootDir = Path.Combine(clusterConfig.ScriptsBasePath, $".{clusterProjectRootDirectory}").Replace('\\', '/');
        string bashSafeRootDir = rootDir.StartsWith("~/") 
            ? "~/" + "\"" + rootDir.Substring(2) + "\"" 
            : "\"" + rootDir + "\"";
        
        var repoUrl = HPCConnectionFrameworkConfiguration.ScriptsSettings.ClusterScriptsRepository;
        var branch = HPCConnectionFrameworkConfiguration.ScriptsSettings.ClusterScriptsRepositoryBranch;
        var sedReplacement = $"{localBasepath}/{clusterConfig.InstanceIdentifierPath}/{clusterConfig.SubExecutionsPath}/{account}";

        if (clusterConfig.SyncScriptsViaSftp)
        {
            return await InitializeClusterScriptDirectoryViaSftpAsync(
                (SshClient)schedulerConnectionConnection,
                repoUrl,
                branch,
                bashSafeRootDir,
                rootDir,
                sedReplacement,
                overwriteExistingProjectRootDirectory);
        }

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

    private async Task<bool> InitializeClusterScriptDirectoryViaSftpAsync(
        SshClient sshClient,
        string repoUrl,
        string branch,
        string bashSafeRootDir,
        string rootDir,
        string sedReplacement,
        bool overwriteExistingProjectRootDirectory)
    {
        try
        {
            // 1. Clone or pull git repo locally on HEAppE server
            var localRepoPath = await CloneOrUpdateRepositoryLocallyAsync(repoUrl, branch);
            var localCommitHash = await GetLocalGitCommitHashAsync(localRepoPath);
            var localKeyScriptsPath = FindKeyScriptsDirectory(localRepoPath);

            if (localKeyScriptsPath == null)
            {
                _logger.LogError($".key_scripts directory not found in local git repository cache: {localRepoPath}");
                return false;
            }

            // 2. Check remote .commit_hash via SSH
            if (!overwriteExistingProjectRootDirectory && !string.IsNullOrEmpty(localCommitHash))
            {
                var checkCmd = $"mkdir -p {bashSafeRootDir}/.key_scripts && cd {bashSafeRootDir} && if [ -f \".key_scripts/.commit_hash\" ] && [ \"$(cat .key_scripts/.commit_hash 2>/dev/null)\" = \"{localCommitHash}\" ]; then echo \"SKIPPED_UP_TO_DATE\"; else echo \"UPDATE_NEEDED\"; fi";
                try
                {
                    var checkResult = await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter(sshClient), checkCmd, _logger);
                    if (checkResult.Result != null && checkResult.Result.Contains("SKIPPED_UP_TO_DATE"))
                    {
                        _logger.LogInformation("Scripts on remote cluster are already up to date (SFTP check). Skipping upload.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Remote script commit hash check failed: {ex.Message}. Proceeding with SFTP upload.");
                }
            }

            // 3. Ensure remote directory exists
            var keyScriptsRootDir = Path.Combine(rootDir, ".key_scripts").Replace('\\', '/');
            string bashSafeKeyScriptsDir = keyScriptsRootDir.StartsWith("~/")
                ? "~/" + "\"" + keyScriptsRootDir.Substring(2) + "\""
                : "\"" + keyScriptsRootDir + "\"";

            var mkdirCmd = $"mkdir -p {bashSafeKeyScriptsDir}";
            await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter(sshClient), mkdirCmd, _logger);

            // 4. Upload files via SFTP (or SSH fallback if SFTP fails/unsupported)
            _logger.LogInformation($"Uploading script files to remote cluster (commit {localCommitHash})...");

            string relativeKeyScriptsDir = rootDir.StartsWith("~/")
                ? rootDir.Substring(2) + "/.key_scripts"
                : rootDir.TrimStart('/') + "/.key_scripts";

            var files = Directory.GetFiles(localKeyScriptsPath);
            var preparedFiles = new Dictionary<string, byte[]>();

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                byte[] fileContent;

                if (fileName == "remote-cmd3.sh")
                {
                    var fileText = await File.ReadAllTextAsync(filePath);
                    fileText = fileText.Replace("TODO", sedReplacement);
                    fileContent = Encoding.UTF8.GetBytes(fileText);
                }
                else
                {
                    fileContent = await File.ReadAllBytesAsync(filePath);
                }
                preparedFiles[fileName] = fileContent;
            }

            bool uploadedSuccessfully = false;
            try
            {
                using var sftpClient = new SftpClient(sshClient.ConnectionInfo);
                sftpClient.HostKeyReceived += (sender, e) => { e.CanTrust = true; };
                sftpClient.Connect();

                foreach (var kvp in preparedFiles)
                {
                    using var memStream = new MemoryStream(kvp.Value);
                    var remoteFilePath = $"{relativeKeyScriptsDir}/{kvp.Key}";
                    sftpClient.UploadFile(memStream, remoteFilePath, true);
                }

                if (!string.IsNullOrEmpty(localCommitHash))
                {
                    using var hashStream = new MemoryStream(Encoding.UTF8.GetBytes(localCommitHash));
                    var remoteHashPath = $"{relativeKeyScriptsDir}/.commit_hash";
                    sftpClient.UploadFile(hashStream, remoteHashPath, true);
                }

                sftpClient.Disconnect();
                uploadedSuccessfully = true;
                _logger.LogInformation("Script directory initialized/updated successfully via SFTP.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"SFTP upload failed ({ex.Message}). Falling back to SSH base64 upload...");
            }

            if (!uploadedSuccessfully)
            {
                try
                {
                    foreach (var kvp in preparedFiles)
                    {
                        var base64 = Convert.ToBase64String(kvp.Value);
                        var remoteFilePath = $"{bashSafeKeyScriptsDir}/{kvp.Key}";
                        var uploadCmd = $"echo '{base64}' | base64 -d > {remoteFilePath}";
                        await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter(sshClient), uploadCmd, _logger);
                    }

                    if (!string.IsNullOrEmpty(localCommitHash))
                    {
                        var hashBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(localCommitHash));
                        var remoteHashPath = $"{bashSafeKeyScriptsDir}/.commit_hash";
                        var uploadHashCmd = $"echo '{hashBase64}' | base64 -d > {remoteHashPath}";
                        await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter(sshClient), uploadHashCmd, _logger);
                    }

                    uploadedSuccessfully = true;
                    _logger.LogInformation("Script directory initialized/updated successfully via SSH base64 fallback.");
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError($"SSH base64 script upload fallback failed: {fallbackEx.Message}");
                    return false;
                }
            }

            // 5. Set executable permissions via SSH
            var chmodCmd = $"mkdir -p {bashSafeKeyScriptsDir} && chmod -R 755 {bashSafeKeyScriptsDir}";
            await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter(sshClient), chmodCmd, _logger);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Script directory initialization failed: {ex.Message}");
            return false;
        }
    }

    private async Task<string> CloneOrUpdateRepositoryLocallyAsync(string repoUrl, string branch)
    {
        var urlBytes = Encoding.UTF8.GetBytes(repoUrl);
        var urlHash = string.Concat(SHA256.HashData(urlBytes).Select(b => b.ToString("x2")));
        var tempCacheDir = Path.Combine(Path.GetTempPath(), "heappe_scripts_cache_" + urlHash);

        _logger.LogInformation($"Local cache directory for git repository: {tempCacheDir}");
        Directory.CreateDirectory(tempCacheDir);

        string gitDir = Path.Combine(tempCacheDir, ".git");
        if (!Directory.Exists(gitDir))
        {
            if (Directory.EnumerateFileSystemEntries(tempCacheDir).Any())
            {
                Directory.Delete(tempCacheDir, true);
                Directory.CreateDirectory(tempCacheDir);
            }

            _logger.LogInformation($"Cloning repository {SanitizeUrl(repoUrl)} (branch: {branch}) locally on HEAppE server...");
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"clone --single-branch -b {branch} \"{repoUrl}\" .",
                WorkingDirectory = tempCacheDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            if (process == null) throw new Exception("Failed to start git clone process.");
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"Failed to clone git repository on HEAppE server: {SanitizeUrl(error)}");
            }
        }
        else
        {
            _logger.LogInformation($"Pulling latest changes for branch {branch} in local cache...");
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"pull origin {branch}",
                WorkingDirectory = tempCacheDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            if (process == null) throw new Exception("Failed to start git pull process.");
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                _logger.LogWarning($"Failed to pull git repository: {SanitizeUrl(error)}. Cleaning directory and re-cloning.");
                Directory.Delete(tempCacheDir, true);
                return await CloneOrUpdateRepositoryLocallyAsync(repoUrl, branch);
            }
        }

        return tempCacheDir;
    }

    private static string SanitizeUrl(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return Regex.Replace(input, @"(?<=https?://)[^@]+@", "***:***@");
    }

    private async Task<string> GetLocalGitCommitHashAsync(string localRepoPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse HEAD",
                WorkingDirectory = localRepoPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                if (process.ExitCode == 0) return output.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to get git commit hash: {ex.Message}");
        }

        return null;
    }

    private static string FindKeyScriptsDirectory(string rootPath)
    {
        var searchPath = Path.Combine(rootPath, "HPC", ".key_scripts");
        if (Directory.Exists(searchPath)) return searchPath;

        searchPath = Path.Combine(rootPath, ".key_scripts");
        if (Directory.Exists(searchPath)) return searchPath;

        var directories = Directory.GetDirectories(rootPath, ".key_scripts", SearchOption.AllDirectories);
        return directories.Length > 0 ? directories[0] : null;
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
            projectBasePath = ExpandPath(projectBasePath, jobInfo, account).Trim();

            var copyFilesClusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
            var heappeJobsDir = $"{copyFilesClusterConfig.InstanceIdentifierPath.TrimStart('/')}/{copyFilesClusterConfig.JobLogArchiveSubPath.TrimStart('/')}";

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
            var src = sourceDestination.Item1?.Trim();
            var dst = sourceDestination.Item2?.Trim();
            cmdBuilder.Append($"if [ -f \"{src}\" ]; then cp \"{src}\" \"{dst}\"; fi;");
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
        return HEAppE.Utils.FileSystemUtils.ExpandRemotePath(path, username);
    }

    private string ExpandPath(string path, SubmittedJobInfo jobInfo, string username)
    {
        return HEAppE.Utils.FileSystemUtils.ExpandRemotePath(path, username, null, jobInfo.Specification.Cluster?.CustomConfiguration);
    }

    #endregion
}