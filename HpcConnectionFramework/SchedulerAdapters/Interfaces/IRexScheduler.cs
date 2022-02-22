using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces
{
    public interface IRexScheduler
    {
        IEnumerable<SubmittedTaskInfo> SubmitJob(JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials);

        IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(IEnumerable<SubmittedTaskInfo> submitedTasksInfo, ClusterAuthenticationCredentials credentials);

        void CancelJob(IEnumerable<string> scheduledJobIds, string message, ClusterAuthenticationCredentials credentials);



        ClusterNodeUsage GetCurrentClusterNodeUsage(ClusterNodeType nodeType);

        IEnumerable<string> GetAllocatedNodes(SubmittedJobInfo jobInfo);

        IEnumerable<string> GetParametersFromGenericUserScript(Cluster cluster, string userScriptPath);

        void AllowDirectFileTransferAccessForUserToJob(string publicKey, SubmittedJobInfo jobInfo);

        void RemoveDirectFileTransferAccessForUserToJob(string publicKey, SubmittedJobInfo jobInfo);

        void CreateJobDirectory(SubmittedJobInfo jobInfo);

        void DeleteJobDirectory(SubmittedJobInfo jobInfo);

        void CopyJobDataToTemp(SubmittedJobInfo jobInfo, string hash, string path);

        void CopyJobDataFromTemp(SubmittedJobInfo jobInfo, string hash);

        void CreateSshTunnel(long jobId, string localHost, int localPort, string loginHost, string nodeHost, int nodePort, ClusterAuthenticationCredentials credentials);

        void RemoveSshTunnel(long jobId, string nodeHost);

        bool SshTunnelExist(long jobId, string nodeHost);
    }
}