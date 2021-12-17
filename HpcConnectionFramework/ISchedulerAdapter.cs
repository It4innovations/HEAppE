using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework {
	public interface ISchedulerAdapter {
        SubmittedJobInfo SubmitJob(object scheduler, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials);

		void CancelJob(object scheduler, string scheduledJobId, string message);

		SubmittedJobInfo GetActualJobInfo(object scheduler, string scheduledJobId);

        SubmittedJobInfo GetActualJobInfo(object scheduler, string[] scheduledJobIds);

        SubmittedTaskInfo[] GetActualTasksInfo(object scheduler, string[] scheduledJobIds);

        SubmittedJobInfo[] GetActualJobsInfo(object scheduler, int[] scheduledJobIds);

		ClusterNodeUsage GetCurrentClusterNodeUsage(object scheduler, ClusterNodeType nodeType);

        List<string> GetAllocatedNodes(object scheduler, SubmittedJobInfo jobInfo);

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