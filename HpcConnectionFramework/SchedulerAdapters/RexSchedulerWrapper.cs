using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using log4net;
using System;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters
{
    public class RexSchedulerWrapper : IRexScheduler
    {
        #region Instances
        /// <summary>
        ///   Reference to the scheduler adapter.
        /// </summary>
        protected ISchedulerAdapter _adapter;

        /// <summary>
        ///   Reference to the scheduler connection pool.
        /// </summary>
        protected IConnectionPool _connectionPool;

        /// <summary>
        /// Logger
        /// </summary>
        protected ILog _log;
        #endregion
        #region Constructors
        public RexSchedulerWrapper(IConnectionPool connectionPool, ISchedulerAdapter adapter)
        {
            _log = LogManager.GetLogger(typeof(RexSchedulerWrapper));
            _connectionPool = connectionPool;
            _adapter = adapter;
        }
        #endregion
        #region IRexScheduler Members
        public IEnumerable<SubmittedTaskInfo> SubmitJob(JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials);
            try
            {
                var tasks = _adapter.SubmitJob(schedulerConnection.Connection, jobSpecification, credentials);
                return tasks;
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }
        public IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(IEnumerable<SubmittedTaskInfo> submitedTasksInfo, ClusterAuthenticationCredentials credentials)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials);
            try
            {
                var tasks = _adapter.GetActualTasksInfo(schedulerConnection.Connection, submitedTasksInfo);
                return tasks;
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        public void CancelJob(IEnumerable<string> scheduledJobIds, string message, ClusterAuthenticationCredentials credentials)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials);
            try
            {
                _adapter.CancelJob(schedulerConnection.Connection, scheduledJobIds, message);
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        public ClusterNodeUsage GetCurrentClusterNodeUsage(ClusterNodeType nodeType)
        {
            Cluster cluster = nodeType.Cluster;
            ClusterAuthenticationCredentials creds = cluster.ServiceAccountCredentials;
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(creds);
            try
            {
                ClusterNodeUsage usage = _adapter.GetCurrentClusterNodeUsage(schedulerConnection.Connection, nodeType);
                return usage;
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        public IEnumerable<string> GetAllocatedNodes(SubmittedJobInfo jobInfo)
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

        public IEnumerable<string> GetParametersFromGenericUserScript(Cluster cluster, string userScriptPath)
        {
            ClusterAuthenticationCredentials creds = cluster.ServiceAccountCredentials;
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(creds);
            try
            {
                return _adapter.GetParametersFromGenericUserScript(schedulerConnection.Connection, userScriptPath);
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        public bool IsWaitingLimitExceeded(SubmittedJobInfo job, JobSpecification jobSpecification)
        {
            int waitingLimit = Convert.ToInt32(jobSpecification.WaitingLimit);
            if (waitingLimit > 0 && (job.State < JobState.Running || job.State == JobState.WaitingForServiceAccount) && job.SubmitTime.HasValue)
                return DateTime.UtcNow.Subtract(job.SubmitTime.Value).TotalSeconds > waitingLimit;
            return false;
        }

        public void AllowDirectFileTransferAccessForUserToJob(string publicKey, SubmittedJobInfo jobInfo)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser);
            try
            {
                _adapter.AllowDirectFileTransferAccessForUserToJob(schedulerConnection.Connection, publicKey, jobInfo);
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        public void RemoveDirectFileTransferAccessForUserToJob(string publicKey, SubmittedJobInfo jobInfo)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser);
            try
            {
                _adapter.RemoveDirectFileTransferAccessForUserToJob(schedulerConnection.Connection, publicKey, jobInfo);
            }
            finally
            {
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
            _adapter.CreateSshTunnel(jobId, localHost, localPort, loginHost, nodeHost, nodePort, credentials);
        }

        public void RemoveSshTunnel(long jobId, string nodeHost)
        {
            _adapter.RemoveSshTunnel(jobId, nodeHost);
        }

        public bool SshTunnelExist(long jobId, string nodeHost)
        {
            return _adapter.SshTunnelExist(jobId, nodeHost);
        }
        #endregion
    }
}