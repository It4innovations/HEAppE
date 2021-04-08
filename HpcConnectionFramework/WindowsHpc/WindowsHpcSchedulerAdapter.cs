using System;
using HaaSMiddleware.DomainObjects.ClusterInformation;
using HaaSMiddleware.DomainObjects.JobManagement;
using HaaSMiddleware.DomainObjects.JobManagement.JobInformation;
using Microsoft.Hpc.Scheduler;
using System.Collections.Generic;

namespace HaaSMiddleware.HpcConnectionFramework.WindowsHpc {
	public class WindowsHpcSchedulerAdapter : ISchedulerAdapter {
		#region Constructors
		public WindowsHpcSchedulerAdapter(ISchedulerDataConvertor convertor) {
			_convertor = convertor;
		}
		#endregion

		#region ISchedulerAdapter Members
		public SubmittedJobInfo SubmitJob(object scheduler, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials) {
			ISchedulerJob job = ((IScheduler) scheduler).CreateJob();
			_convertor.ConvertJobSpecificationToJob(jobSpecification, job);
			((IScheduler) scheduler).AddJob(job);
			((IScheduler) scheduler).SubmitJobById(Convert.ToInt32(job.Id), credentials.Username, credentials.Password);
			//UpdateWorkDirectoriesForTasks(job, jobSpecification);
			return _convertor.ConvertJobToJobInfo(job);
		}

		public void CancelJob(object scheduler, int scheduledJobId, string message) {
			((IScheduler) scheduler).CancelJob(Convert.ToInt32(scheduledJobId), message);
		}

		public SubmittedJobInfo GetActualJobInfo(object scheduler, int scheduledJobId) {
			ISchedulerJob job = ((IScheduler) scheduler).OpenJob(Convert.ToInt32(scheduledJobId));
			return _convertor.ConvertJobToJobInfo(job);
		}

		public ClusterNodeUsage GetCurrentClusterNodeUsage(object scheduler, ClusterNodeType nodeType) {
			throw new NotImplementedException();
		}

        public List<string> GetAllocatedNodes(object scheduler, SubmittedJobInfo jobInfo)
        {
            throw new NotImplementedException();
        }

		public void AllowDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo) {
			throw new NotImplementedException();
		}

		public void RemoveDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo) {
			throw new NotImplementedException();
		}

		public SubmittedJobInfo[] GetActualJobsInfo(object scheduler, int[] scheduledJobIds) {
			throw new NotImplementedException();
		}

		public void CreateJobDirectory(object scheduler, SubmittedJobInfo jobInfo)	{
			throw new NotImplementedException();
		}

        public void DeleteJobDirectory(object scheduler, SubmittedJobInfo jobInfo)
        {
            throw new NotImplementedException();
        }

        public void CopyJobDataToTemp(object scheduler, SubmittedJobInfo jobInfo, string hash, string path)
        {
            throw new NotImplementedException();
        }

        public void CopyJobDataFromTemp(object scheduler, SubmittedJobInfo jobInfo, string hash)
        {
            throw new NotImplementedException();
        }

        public void CreateSshTunnel(long jobId, string localHost, int localPort, string loginHost, string nodeHost, int nodePort, ClusterAuthenticationCredentials credentials)
        {
            throw new NotImplementedException();
        }

        public void RemoveSshTunnel(long jobId, string nodeHost)
        {
            throw new NotImplementedException();
        }

        public bool SshTunnelExist(long jobId, string nodeHost)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Local Methods
        /*protected void UpdateWorkDirectoriesForTasks(Microsoft.Hpc.Scheduler.ISchedulerJob job, FullJobSpecification jobSpecification)
		{
			string jobClusterDirectory = FileSystemUtils.GetJobClusterDirectoryPath(jobSpecification.ClusterLocalWorkPath, jobSpecification.JobRelativeWorkDirectory);
			foreach (Microsoft.Hpc.Scheduler.ISchedulerTask task in job.GetTaskList(null, null, false))
			{
				task.WorkDirectory = FileSystemUtils.GetTaskClusterDirectoryPath(jobClusterDirectory, task.WorkDirectory);
				task.Commit();
			}
		}*/
        #endregion

        #region Instance Fields
        /// <summary>
        ///   Convertor reference.
        /// </summary>
        protected ISchedulerDataConvertor _convertor;
		#endregion


		
	}
}