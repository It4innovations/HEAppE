using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.HpcConnectionFramework.SystemCommands;

/// <summary>
///     ISystem commands
/// </summary>
public interface ICommands
{
    /// <summary>
    ///     System interpreter command
    /// </summary>
    string InterpreterCommand { get; }

    /// <summary>
    ///     Get generic command templates parameters from script
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="userScriptPath">Generic script path</param>
    /// <returns></returns>
    Task<IEnumerable<string>> GetParametersFromGenericUserScriptAsync(object connectorClient, string userScriptPath);

    /// <summary>
    ///     Copy job data to temp folder
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job information</param>
    /// <param name="hash">Hash</param>
    /// <param name="path">Path</param>
    Task CopyJobDataToTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash,
        string path);

    /// <summary>
    ///     Copy job data from temp folder
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job information</param>
    /// <param name="hash">Hash</param>
    Task CopyJobDataFromTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash);

    /// <summary>
    ///     Allow direct file transfer acces for user
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="publicKey">Public key</param>
    /// <param name="jobInfo">Job information</param>
    Task AllowDirectFileTransferAccessForUserToJobAsync(object connectorClient, string publicKey, SubmittedJobInfo jobInfo);

    /// <summary>
    ///     Remove direct file transfer acces for user
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="publicKeys">Public keys</param>
    Task RemoveDirectFileTransferAccessForUserAsync(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString);

    /// <summary>
    ///     Create job directory
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job information</param>
    /// <param name="localBasePath"></param>
    /// <param name="sharedAccountsPoolMode"></param>
    Task CreateJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode);

    /// <summary>
    ///     Delete job directory
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job information</param>
    Task<bool> DeleteJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath);

    /// <summary>
    ///     Initialize Cluster Script Directory
    /// </summary>
    /// <param name="schedulerConnectionConnection">Connector</param>
    /// <param name="clusterProjectRootDirectory">Cluster project root path</param>
    /// <param name="overwriteExistingProjectRootDirectory">Overwrite existing scripts directory</param>
    /// <param name="localBasepath">Cluster execution path</param>
    /// <param name="isServiceAccount">Is servis account</param>
    /// <param name="account">Cluster username</param>
    Task<bool> InitializeClusterScriptDirectoryAsync(object schedulerConnectionConnection, string clusterProjectRootDirectory, 
        bool overwriteExistingProjectRootDirectory, string localBasepath, string account, bool isServiceAccount);

    Task<bool> CopyJobFilesAsync(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations);
}