using HEAppE.DomainObjects.JobManagement.JobInformation;
using System;

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
        string ExecutieCmdScriptPath { get; }

        /// <summary>
        /// Copy job data to temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        /// <param name="path">Path</param>
        void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash, string path);

        /// <summary>
        /// Copy job data from temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash);

        /// <summary>
        /// Allow direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job info</param>
        void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo);

        /// <summary>
        /// Remove direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Conenctor</param>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job info</param>
        void RemoveDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo);

        /// <summary>
        /// Create job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo);

        /// <summary>
        /// Delete job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        void DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo);
    }
}
