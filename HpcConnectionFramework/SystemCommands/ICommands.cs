using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SystemCommands
{
    /// <summary>
    /// ISystem commands
    /// </summary>
    public interface ICommands
    {
        /// <summary>
        /// System interpreter command
        /// </summary>
        string InterpreterCommand { get; }

        /// <summary>
        /// Execution command script path
        /// </summary>
        string ExecuteCmdScriptPath { get; }

        /// <summary>
        /// Get generic command templates parameters from script
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="userScriptPath">Generic script path</param>
        /// <returns></returns>
        IEnumerable<string> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath);

        /// <summary>
        /// Copy job data to temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        /// <param name="hash">Hash</param>
        /// <param name="path">Path</param>
        void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash, string path);

        /// <summary>
        /// Copy job data from temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        /// <param name="hash">Hash</param>
        void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash);

        /// <summary>
        /// Allow direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job information</param>
        void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo);

        /// <summary>
        /// Remove direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKeys">Public keys</param>
        void RemoveDirectFileTransferAccessForUser(object connectorClient, IEnumerable<string> publicKeys);

        /// <summary>
        /// Create job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        /// <param name="localBasePath"></param>
        /// <param name="sharedAccountsPoolMode"></param>
        /// <param name="serviceAccountUsername"></param>
        void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
            bool sharedAccountsPoolMode, string serviceAccountUsername);

        /// <summary>
        /// Delete job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        void DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath);

        /// <summary>
        /// Initialize cluster script directory
        /// </summary>
        /// <param name="schedulerConnectionConnection"></param>
        /// <param name="clusterProjectRootDirectory"></param>
        /// <param name="localBasepath"></param>
        /// <returns></returns>
        string InitializeClusterScriptDirectory(object schedulerConnectionConnection,
            string clusterProjectRootDirectory, string localBasepath);
    }
}
