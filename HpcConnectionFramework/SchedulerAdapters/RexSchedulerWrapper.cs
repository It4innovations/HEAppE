using System;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using log4net;
using System.Collections.Generic;
using System.Linq;
using Exception = System.Exception;

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
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials, jobSpecification.Cluster);
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
            var cluster = submitedTasksInfo.FirstOrDefault().Specification.JobSpecification.Cluster;
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials, cluster);
            try
            {
                var tasks = _adapter.GetActualTasksInfo(schedulerConnection.Connection, cluster, submitedTasksInfo);
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
            var cluster = submitedTasksInfo.FirstOrDefault().Specification.JobSpecification.Cluster;
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials, cluster);
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
        public ClusterNodeUsage GetCurrentClusterNodeUsage(ClusterNodeType nodeType, ClusterAuthenticationCredentials credentials)
        {
            Cluster cluster = nodeType.Cluster;
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials, cluster);
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
        /// Get allocated nodes address for task
        /// </summary>
        /// <param name="taskInfo">Task information</param>
        public IEnumerable<string> GetAllocatedNodes(SubmittedTaskInfo taskInfo)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(taskInfo.Specification.JobSpecification.ClusterUser, taskInfo.Specification.JobSpecification.Cluster);
            try
            {
                return _adapter.GetAllocatedNodes(schedulerConnection.Connection, taskInfo);
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
        public IEnumerable<string> GetParametersFromGenericUserScript(Cluster cluster, ClusterAuthenticationCredentials serviceCredentials, string userScriptPath)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(serviceCredentials, cluster);
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
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster);
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
        /// <param name="publicKeys">Public keys</param>
        /// <param name="credentials">Credentials</param>
        public void RemoveDirectFileTransferAccessForUser(IEnumerable<string> publicKeys, ClusterAuthenticationCredentials credentials, Cluster cluster)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(credentials, cluster);
            try
            {
                _adapter.RemoveDirectFileTransferAccessForUser(schedulerConnection.Connection, publicKeys);
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
        /// <param name="localBasePath"></param>
        /// <param name="sharedAccountsPoolMode"></param>
        public void CreateJobDirectory(SubmittedJobInfo jobInfo, string localBasePath, bool sharedAccountsPoolMode)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster);
            try
            {
                
                _adapter.CreateJobDirectory(schedulerConnection.Connection, jobInfo, localBasePath, sharedAccountsPoolMode);
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
        public void DeleteJobDirectory(SubmittedJobInfo jobInfo, string localBasePath)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster);
            try
            {
                _adapter.DeleteJobDirectory(schedulerConnection.Connection, jobInfo, localBasePath);
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
        public void CopyJobDataToTemp(SubmittedJobInfo jobInfo, string localBasePath, string hash, string path)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster);
            try
            {
                _adapter.CopyJobDataToTemp(schedulerConnection.Connection, jobInfo, localBasePath, hash, path);
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
        public void CopyJobDataFromTemp(SubmittedJobInfo jobInfo, string localBasePath, string hash)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster);
            try
            {
                _adapter.CopyJobDataFromTemp(schedulerConnection.Connection, jobInfo, localBasePath, hash);
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        /// <summary>
        /// Create tunnel
        /// </summary>
        /// <param name="taskInfo">Task info</param>
        /// <param name="nodeHost">Cluster node address</param>
        /// <param name="nodePort">Cluster node port</param>
        public void CreateTunnel(SubmittedTaskInfo taskInfo, string nodeHost, int nodePort)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(taskInfo.Specification.JobSpecification.ClusterUser, taskInfo.Specification.JobSpecification.Cluster);
            try
            {
                _adapter.CreateTunnel(schedulerConnection.Connection, taskInfo, nodeHost, nodePort);
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        /// <summary>
        /// Remove tunnel
        /// </summary>
        /// <param name="taskInfo">Task info</param>
        public void RemoveTunnel(SubmittedTaskInfo taskInfo)
        {

            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(taskInfo.Specification.JobSpecification.ClusterUser, taskInfo.Specification.JobSpecification.Cluster);
            try
            {
                _adapter.RemoveTunnel(schedulerConnection, taskInfo);
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        /// <summary>
        /// Get tunnels information
        /// </summary>
        /// <param name="taskInfo">Task info</param>
        /// <param name="nodeHost">Node host</param>
        /// <returns></returns>
        public IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost)
        {
            return _adapter.GetTunnelsInfos(taskInfo, nodeHost);
        }

        public string InitializeClusterScriptDirectory(string clusterProjectRootDirectory, string localBasepath, Cluster cluster, ClusterAuthenticationCredentials clusterAuthCredentials)
        {
            ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(clusterAuthCredentials, cluster);
            try
            {
                return _adapter.InitializeClusterScriptDirectory(schedulerConnection.Connection, clusterProjectRootDirectory, localBasepath);
            }
            finally
            {
                _connectionPool.ReturnConnection(schedulerConnection);
            }
        }

        public bool TestClusterAccessForAccount(Cluster cluster, ClusterAuthenticationCredentials clusterAuthCredentials)
        {
            try
            {
                ConnectionInfo schedulerConnection = _connectionPool.GetConnectionForUser(clusterAuthCredentials, cluster);
                return true;
            }
            catch (Exception)
            {
                _log.Info($"Cluster access test failed for project {clusterAuthCredentials.ClusterProjectCredentials.First().ClusterProject.ProjectId}");
                return false;
            }
        }

        #endregion
    }
}