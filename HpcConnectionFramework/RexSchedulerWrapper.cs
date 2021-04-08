using System;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework {
	public class RexSchedulerWrapper : IRexScheduler {
		public RexSchedulerWrapper(IConnectionPool connectionPool, ISchedulerAdapter adapter) {
			_connectionPool = connectionPool;
			_adapter = adapter;
		}

		public SubmittedJobInfo SubmitJob(JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials) {
			ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials);
			try {
				SubmittedJobInfo info = _adapter.SubmitJob(schedulerConnection.Connection, jobSpecification, credentials);
				return info;
			}
			finally {
				_connectionPool.ReturnConnection(schedulerConnection);
			}
		}

		public void CancelJob(string scheduledJobId, ClusterAuthenticationCredentials credentials) {
			CancelJob(scheduledJobId, "Job cancelled manually by the client.", credentials);
		}

        public void CancelJob(string scheduledJobId, string message, ClusterAuthenticationCredentials credentials) {
			ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials);
			try {
				_adapter.CancelJob(schedulerConnection.Connection, scheduledJobId, message);
			}
			finally {
				_connectionPool.ReturnConnection(schedulerConnection);
			}
		}

		public SubmittedJobInfo GetActualJobInfo(string scheduledJobId, ClusterAuthenticationCredentials credentials) {
			ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials);
			try {
				SubmittedJobInfo jobInfo = _adapter.GetActualJobInfo(schedulerConnection.Connection, scheduledJobId);
				/*MessageLogger.Log("GetActualJobInfo.AllParameters:");
        MessageLogger.Log(jobInfo.AllParameters);*/
				return jobInfo;
			}
			finally {
				_connectionPool.ReturnConnection(schedulerConnection);
			}
		}

		public SubmittedJobInfo[] GetActualJobsInfo(int[] scheduledJobIds, Cluster cluster) {
			ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(cluster.ServiceAccountCredentials);
			try {
				SubmittedJobInfo[] jobInfoArray = _adapter.GetActualJobsInfo(schedulerConnection.Connection, scheduledJobIds);
				return jobInfoArray;
			}
			finally {
				_connectionPool.ReturnConnection(schedulerConnection);
			}
		}

        public SubmittedTaskInfo[] GetActualTasksInfo(string[] scheduledJobIds, Cluster cluster)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(cluster.ServiceAccountCredentials);
            try
            {
                SubmittedTaskInfo[] taskInfoArray = _adapter.GetActualTasksInfo(schedulerConnection.Connection, scheduledJobIds);
                return taskInfoArray;
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        public ClusterNodeUsage GetCurrentClusterNodeUsage(ClusterNodeType nodeType) {
			Cluster cluster = nodeType.Cluster;
			ClusterAuthenticationCredentials creds = cluster.ServiceAccountCredentials;
			ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(creds);
			try {
				ClusterNodeUsage usage = _adapter.GetCurrentClusterNodeUsage(schedulerConnection.Connection, nodeType);
				return usage;
			}
			finally {
				_connectionPool.ReturnConnection(schedulerConnection);
			}
		}

        public List<string> GetAllocatedNodes(SubmittedJobInfo jobInfo)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser);
            try
            {
                return _adapter.GetAllocatedNodes(schedulerConnection.Connection, jobInfo);
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

		public bool IsWaitingLimitExceeded(SubmittedJobInfo job, JobSpecification jobSpecification) {
			int waitingLimit = Convert.ToInt32(jobSpecification.WaitingLimit);
			if ((waitingLimit > 0) && (job.State < JobState.Running) && (job.SubmitTime.HasValue))
				return DateTime.UtcNow.Subtract(job.SubmitTime.Value).TotalSeconds > waitingLimit;
			return false;
		}

		public void AllowDirectFileTransferAccessForUserToJob(string publicKey, SubmittedJobInfo jobInfo) {
			ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser);
			try {
				_adapter.AllowDirectFileTransferAccessForUserToJob(schedulerConnection.Connection, publicKey, jobInfo);
			}
			finally {
				_connectionPool.ReturnConnection(schedulerConnection);
			}
		}

		public void RemoveDirectFileTransferAccessForUserToJob(string publicKey, SubmittedJobInfo jobInfo) {
			ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser);
			try {
				_adapter.RemoveDirectFileTransferAccessForUserToJob(schedulerConnection.Connection, publicKey, jobInfo);
			}
			finally {
				_connectionPool.ReturnConnection(schedulerConnection);
			}
		}

		public void CreateJobDirectory(SubmittedJobInfo jobInfo)
		{
			ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser);
			try
			{
				_adapter.CreateJobDirectory(schedulerConnection.Connection, jobInfo);
			}
			finally
			{
				_connectionPool.ReturnConnection(schedulerConnection);
			}
		}

        public void DeleteJobDirectory(SubmittedJobInfo jobInfo)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser);
            try
            {
                _adapter.DeleteJobDirectory(schedulerConnection.Connection, jobInfo);
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        public void CopyJobDataToTemp(SubmittedJobInfo jobInfo, string hash, string path)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser);
            try
            {
                _adapter.CopyJobDataToTemp(schedulerConnection.Connection, jobInfo, hash, path);
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        public void CopyJobDataFromTemp(SubmittedJobInfo jobInfo, string hash)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser);
            try
            {
                _adapter.CopyJobDataFromTemp(schedulerConnection.Connection, jobInfo, hash);
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        public void CreateSshTunnel(long jobId, string localHost, int localPort, string loginHost, string nodeHost, int nodePort, ClusterAuthenticationCredentials credentials)
        {
            //ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials);
            try
            {
                _adapter.CreateSshTunnel(jobId, localHost, localPort, loginHost, nodeHost, nodePort, credentials);
            }
            finally
            {
                //_connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        public void RemoveSshTunnel(long jobId, string nodeHost)
        {
            //ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials);
            try
            {
                _adapter.RemoveSshTunnel(jobId, nodeHost);
            }
            finally
            {
                //_connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        public bool SshTunnelExist(long jobId, string nodeHost)
        {
            //ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials);
            try
            {
                return _adapter.SshTunnelExist(jobId, nodeHost);
            }
            finally
            {
                //_connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        #region Instance Fields
        /// <summary>
        ///   Reference to the scheduler adapter.
        /// </summary>
        protected ISchedulerAdapter _adapter;

		/// <summary>
		///   Reference to the scheduler connection pool.
		/// </summary>
		protected IConnectionPool _connectionPool;
		#endregion
}
}