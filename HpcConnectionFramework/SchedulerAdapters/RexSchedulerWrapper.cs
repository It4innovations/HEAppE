using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using log4net;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters
{
    /// <summary>
    /// Rex scheduler wrapper
    /// </summary>
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
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionPool">Connection pool</param>
        /// <param name="adapter">Scheduler adapter</param>
        public RexSchedulerWrapper(IConnectionPool connectionPool, ISchedulerAdapter adapter)
        {
            _log = LogManager.GetLogger(typeof(RexSchedulerWrapper));
            _connectionPool = connectionPool;
            _adapter = adapter;
        }
        #endregion
        #region IRexScheduler Members
        /// <summary>
        /// Submit job to scheduler
        /// </summary>
        /// <param name="jobSpecification">Job specification</param>
        /// <param name="credentials">Credentials</param>
        /// <returns></returns>
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
        /// <summary>
        /// Get actual tasks
        /// </summary>
        /// <param name="submitedTasksInfo">Submitted tasks ids</param>
        /// <param name="credentials">Credentials</param>
        /// <returns></returns>
        public IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(IEnumerable<SubmittedTaskInfo> submitedTasksInfo, ClusterAuthenticationCredentials credentials)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials);
            try
            {
                var tasks = _adapter.GetActualTasksInfo(schedulerConnection.Connection, credentials.Cluster, submitedTasksInfo);
                return tasks;
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        /// <summary>
        /// Cancel job
        /// </summary>
        /// <param name="submitedTasksInfo">Submitted tasks id´s</param>
        /// <param name="message">Message</param>
        /// <param name="credentials">Credentials</param>
        public void CancelJob(IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message, ClusterAuthenticationCredentials credentials)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials);
            try
            {
                _adapter.CancelJob(schedulerConnection.Connection, submitedTasksInfo, message);
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        /// <summary>
        /// Get actual scheduler queue status
        /// </summary>
        /// <param name="nodeType">Cluster node type</param>
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

        /// <summary>
        /// Get allocated nodes per job
        /// </summary>
        /// <param name="jobInfo">Job information</param>
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

        /// <summary>
        /// Get generic command templates parameters from script
        /// </summary>
        /// <param name="cluster">Cluster</param>
        /// <param name="userScriptPath">Generic script path</param>
        /// <returns></returns>
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

        /// <summary>
        /// Allow direct file transfer acces for user
        /// </summary>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job info</param>
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

        /// <summary>
        /// Remove direct file transfer access for user
        /// </summary>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job info</param>
        public void RemoveDirectFileTransferAccessForUserToJob(string publicKey, SubmittedJobInfo jobInfo)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser);
            try
            {
                _adapter.RemoveDirectFileTransferAccessForUserToJob(schedulerConnection.Connection, publicKey);
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        /// <summary>
        /// Create job directory
        /// </summary>
        /// <param name="jobInfo">Job info</param>
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

        /// <summary>
        /// Delete job directory
        /// </summary>
        /// <param name="jobInfo">Job info</param>
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

        /// <summary>
        /// Copy job data to temp folder
        /// </summary>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        /// <param name="path">Path</param>
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

        /// <summary>
        /// Copy job data from temp folder
        /// </summary>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
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

        /// <summary>
        /// Create SSH tunnel
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="localHost">Local host</param>
        /// <param name="localPort">Local port</param>
        /// <param name="loginHost">Login host</param>
        /// <param name="nodeHost">Node host</param>
        /// <param name="nodePort">Node port</param>
        /// <param name="credentials">Credentials</param>
        public void CreateSshTunnel(long jobId, string localHost, int localPort, string loginHost, string nodeHost, int nodePort, ClusterAuthenticationCredentials credentials)
        {
            _adapter.CreateSshTunnel(jobId, localHost, localPort, loginHost, nodeHost, nodePort, credentials);
        }

        /// <summary>
        /// Remove SSH tunnel
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="nodeHost">Node host</param>
        public void RemoveSshTunnel(long jobId, string nodeHost)
        {
            _adapter.RemoveSshTunnel(jobId, nodeHost);
        }

        /// <summary>
        /// Check if SSH tunnel exist
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="nodeHost">Node host</param>
        /// <returns></returns>
        public bool SshTunnelExist(long jobId, string nodeHost)
        {
            return _adapter.SshTunnelExist(jobId, nodeHost);
        }
        #endregion
    }
}