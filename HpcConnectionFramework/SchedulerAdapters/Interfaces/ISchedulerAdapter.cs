using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces
{
    public interface ISchedulerAdapter
    {
        IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials);

        IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo);

        void CancelJob(object connectorClient, IEnumerable<string> scheduledJobIds, string message);

        ClusterNodeUsage GetCurrentClusterNodeUsage(object scheduler, ClusterNodeType nodeType);

        IEnumerable<string> GetAllocatedNodes(object scheduler, SubmittedJobInfo jobInfo);

        IEnumerable<string> GetParametersFromGenericUserScript(object scheduler, string userScriptPath);

        void AllowDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo);

        void RemoveDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo);

        void CreateJobDirectory(object scheduler, SubmittedJobInfo jobInfo);

        void DeleteJobDirectory(object scheduler, SubmittedJobInfo jobInfo);

        void CopyJobDataToTemp(object scheduler, SubmittedJobInfo jobInfo, string hash, string path);

        void CopyJobDataFromTemp(object scheduler, SubmittedJobInfo jobInfo, string hash);

        void CreateSshTunnel(long jobId, string localHost, int localPort, string loginHost, string nodeHost, int nodePort, ClusterAuthenticationCredentials credentials);

        void RemoveSshTunnel(long jobId, string nodeHost);

        bool SshTunnelExist(long jobId, string nodeHost);
    }
}