using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces
{
    /// <summary>
    /// IScheduler adapter
    /// </summary>
    public interface ISchedulerAdapter
    {
        IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials);

        IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster, IEnumerable<SubmittedTaskInfo> submitedTasksInfo);

        void CancelJob(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message);

        ClusterNodeUsage GetCurrentClusterNodeUsage(object connectorClient, ClusterNodeType nodeType);

        IEnumerable<string> GetAllocatedNodes(object connectorClient, SubmittedTaskInfo taskInfo);

        IEnumerable<string> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath);

        void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo);

        void RemoveDirectFileTransferAccessForUser(object connectorClient, IEnumerable<string> publicKeys);

        void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, bool sharedAccountsPoolMode);

        void DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath);

        void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash, string path);

        void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash, string localBasePath);

        void CreateTunnel(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort);

        void RemoveTunnel(object connectorClient, SubmittedTaskInfo taskInfo);

        IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost);
        bool InitializeClusterScriptDirectory(object schedulerConnectionConnection, string clusterProjectRootDirectory, string localBasepath, bool isServiceAccount);
    }
}