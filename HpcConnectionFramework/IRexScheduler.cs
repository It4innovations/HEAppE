// CSharpServer.cs
// compile with: /target:library
// post-build command: regasm CSharpServer.dll /tlb:CSharpServer.tlb

using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework {
	public interface IRexScheduler {
		SubmittedJobInfo SubmitJob(JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials);

		void CancelJob(string scheduledJobId, ClusterAuthenticationCredentials credentials);

		void CancelJob(string scheduledJobId, string message, ClusterAuthenticationCredentials credentials);

		SubmittedJobInfo GetActualJobInfo(string scheduledJobId, ClusterAuthenticationCredentials credentials);

		SubmittedJobInfo[] GetActualJobsInfo(int[] scheduledJobIds, Cluster cluster);

        SubmittedTaskInfo[] GetActualTasksInfo(string[] scheduledJobIds, Cluster cluster);

        ClusterNodeUsage GetCurrentClusterNodeUsage(ClusterNodeType nodeType);

        List<string> GetAllocatedNodes(SubmittedJobInfo jobInfo);

        IEnumerable<string> GetParametersFromGenericUserScript(Cluster cluster, string userScriptPath);

        bool IsWaitingLimitExceeded(SubmittedJobInfo job, JobSpecification jobSpecification);

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