using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using log4net;
using Exception = System.Exception;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters;

/// <summary>
///     Rex scheduler wrapper
/// </summary>
public class RexSchedulerWrapper : IRexScheduler
{
    #region Constructors

    /// <summary>
    ///     Constructor
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

    #region Instances

    /// <summary>
    ///     Reference to the scheduler adapter.
    /// </summary>
    protected ISchedulerAdapter _adapter;

    /// <summary>
    ///     Reference to the scheduler connection pool.
    /// </summary>
    protected IConnectionPool _connectionPool;

    /// <summary>
    ///     Logger
    /// </summary>
    protected ILog _log;

    #endregion

    #region IRexScheduler Members

    /// <summary>
    ///     Submit job to scheduler
    /// </summary>
    /// <param name="jobSpecification">Job specification</param>
    /// <param name="credentials">Credentials</param>
    /// <returns></returns>
    public IEnumerable<SubmittedTaskInfo> SubmitJob(JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials)
    {
        var schedulerConnection = _connectionPool.GetConnectionForUser(credentials, jobSpecification.Cluster);
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
    ///     Get actual tasks
    /// </summary>
    /// <param name="submitedTasksInfo">Submitted tasks ids</param>
    /// <param name="credentials">Credentials</param>
    /// <returns></returns>
    public IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(IEnumerable<SubmittedTaskInfo> submitedTasksInfo,
        ClusterAuthenticationCredentials credentials)
    {
        var cluster = submitedTasksInfo.FirstOrDefault().Specification.JobSpecification.Cluster;
        var schedulerConnection = _connectionPool.GetConnectionForUser(credentials, cluster);
        try
        {
            var allTasks = new List<SubmittedTaskInfo>();
            var groupedTasksByUser = submitedTasksInfo
                .GroupBy(t => t.Specification.JobSpecification.ClusterUser.Username);

            foreach (var groupedTasksByUsername in groupedTasksByUser)
            {
                var tasks = _adapter.GetActualTasksInfo(schedulerConnection.Connection, cluster, groupedTasksByUsername.ToList(), groupedTasksByUsername.Key);
                allTasks.AddRange(tasks);
            }

            return allTasks;
        }
        finally
        {
            _connectionPool.ReturnConnection(schedulerConnection);
        }
    }

    /// <summary>
    ///     Cancel job
    /// </summary>
    /// <param name="submitedTasksInfo">Submitted tasks id´s</param>
    /// <param name="message">Message</param>
    /// <param name="credentials">Credentials</param>
    public void CancelJob(IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message,
        ClusterAuthenticationCredentials credentials)
    {
        var cluster = submitedTasksInfo.FirstOrDefault().Specification.JobSpecification.Cluster;
        var schedulerConnection = _connectionPool.GetConnectionForUser(credentials, cluster);
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
    ///     Get actual scheduler queue status
    /// </summary>
    /// <param name="nodeType">Cluster node type</param>
    public ClusterNodeUsage GetCurrentClusterNodeUsage(ClusterNodeType nodeType,
        ClusterAuthenticationCredentials credentials)
    {
        var cluster = nodeType.Cluster;
        var schedulerConnection = _connectionPool.GetConnectionForUser(credentials, cluster);
        try
        {
            var usage = _adapter.GetCurrentClusterNodeUsage(schedulerConnection.Connection, nodeType);
            return usage;
        }
        finally
        {
            _connectionPool.ReturnConnection(schedulerConnection);
        }
    }

    /// <summary>
    ///     Get allocated nodes address for task
    /// </summary>
    /// <param name="taskInfo">Task information</param>
    public IEnumerable<string> GetAllocatedNodes(SubmittedTaskInfo taskInfo)
    {
        var schedulerConnection = _connectionPool.GetConnectionForUser(
            taskInfo.Specification.JobSpecification.ClusterUser, taskInfo.Specification.JobSpecification.Cluster);
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
    ///     Get generic command templates parameters from script
    /// </summary>
    /// <param name="cluster">Cluster</param>
    /// <param name="userScriptPath">Generic script path</param>
    /// <returns></returns>
    public IEnumerable<string> GetParametersFromGenericUserScript(Cluster cluster,
        ClusterAuthenticationCredentials serviceCredentials, string userScriptPath)
    {
        var schedulerConnection = _connectionPool.GetConnectionForUser(serviceCredentials, cluster);
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
    ///     Allow direct file transfer acces for user
    /// </summary>
    /// <param name="publicKey">Public key</param>
    /// <param name="jobInfo">Job info</param>
    public void AllowDirectFileTransferAccessForUserToJob(string publicKey, SubmittedJobInfo jobInfo)
    {
        var schedulerConnection =
            _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster);
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
    ///     Remove direct file transfer access for user
    /// </summary>
    /// <param name="publicKeys">Public keys</param>
    /// <param name="credentials">Credentials</param>
    public void RemoveDirectFileTransferAccessForUser(IEnumerable<string> publicKeys,
        ClusterAuthenticationCredentials credentials, Cluster cluster, Project project)
    {
        var schedulerConnection = _connectionPool.GetConnectionForUser(credentials, cluster);
        try
        {
            _adapter.RemoveDirectFileTransferAccessForUser(schedulerConnection.Connection, publicKeys, project.AccountingString);
        }
        finally
        {
            _connectionPool.ReturnConnection(schedulerConnection);
        }
    }

    /// <summary>
    ///     Create job directory
    /// </summary>
    /// <param name="jobInfo">Job info</param>
    /// <param name="localBasePath"></param>
    /// <param name="sharedAccountsPoolMode"></param>
    public void CreateJobDirectory(SubmittedJobInfo jobInfo, string localBasePath, bool sharedAccountsPoolMode)
    {
        var schedulerConnection =
            _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster);
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
    ///     Delete job directory
    /// </summary>
    /// <param name="jobInfo">Job info</param>
    public bool DeleteJobDirectory(SubmittedJobInfo jobInfo, string localBasePath)
    {
        var schedulerConnection =
            _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster);
        try
        {
            return _adapter.DeleteJobDirectory(schedulerConnection.Connection, jobInfo, localBasePath);
        }
        catch (Exception ex)
        {
            _log.Error($"Error deleting job directory for job {jobInfo.Id}", ex);
            return false;
        }
        finally
        {
            _connectionPool.ReturnConnection(schedulerConnection);
        }
    }

    /// <summary>
    ///     Copy job data to temp folder
    /// </summary>
    /// <param name="jobInfo">Job info</param>
    /// <param name="hash">Hash</param>
    /// <param name="path">Path</param>
    public void CopyJobDataToTemp(SubmittedJobInfo jobInfo, string localBasePath, string hash, string path)
    {
        var schedulerConnection =
            _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster);
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
    ///     Copy job data from temp folder
    /// </summary>
    /// <param name="jobInfo">Job info</param>
    /// <param name="hash">Hash</param>
    public void CopyJobDataFromTemp(SubmittedJobInfo jobInfo, string localBasePath, string hash)
    {
        var schedulerConnection =
            _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster);
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
    ///     Create tunnel
    /// </summary>
    /// <param name="taskInfo">Task info</param>
    /// <param name="nodeHost">Cluster node address</param>
    /// <param name="nodePort">Cluster node port</param>
    public void CreateTunnel(SubmittedTaskInfo taskInfo, string nodeHost, int nodePort)
    {
        var schedulerConnection = _connectionPool.GetConnectionForUser(
            taskInfo.Specification.JobSpecification.ClusterUser, taskInfo.Specification.JobSpecification.Cluster);
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
    ///     Remove tunnel
    /// </summary>
    /// <param name="taskInfo">Task info</param>
    public void RemoveTunnel(SubmittedTaskInfo taskInfo)
    {
        var schedulerConnection = _connectionPool.GetConnectionForUser(
            taskInfo.Specification.JobSpecification.ClusterUser, taskInfo.Specification.JobSpecification.Cluster);
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
    ///     Get tunnels information
    /// </summary>
    /// <param name="taskInfo">Task info</param>
    /// <param name="nodeHost">Node host</param>
    /// <returns></returns>
    public IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost)
    {
        return _adapter.GetTunnelsInfos(taskInfo, nodeHost);
    }

    /// <summary>
    ///     Initialize Cluster Script Directory
    /// </summary>
    /// <param name="clusterProjectRootDirectory">Cluster project root path</param>
    /// <param name="overwriteExistingProjectRootDirectory">Overwrite existing scripts directory</param>
    /// <param name="localBasepath">Cluster execution path</param>
    /// <param name="clusterAuthCredentials">Credentials</param>
    /// <param name="isServiceAccount">Is servis account</param>
    public bool InitializeClusterScriptDirectory(string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath,
        Cluster cluster, ClusterAuthenticationCredentials clusterAuthCredentials, bool isServiceAccount)
    {
        ConnectionInfo schedulerConnection = null;
        try
        {
            schedulerConnection = _connectionPool.GetConnectionForUser(clusterAuthCredentials, cluster);
            return _adapter.InitializeClusterScriptDirectory(schedulerConnection.Connection,
                clusterProjectRootDirectory, overwriteExistingProjectRootDirectory, localBasepath, clusterAuthCredentials.Username, isServiceAccount);
        }
        catch (Exception ex)
        {
            _log.Error(
                $"Cluster script directory initialization failed for project {clusterAuthCredentials.ClusterProjectCredentials.First().ClusterProject.ProjectId}, {ex.Message}",
                ex);
            return false;
        }
        finally
        {
            if (schedulerConnection != null) _connectionPool.ReturnConnection(schedulerConnection);
        }
    }

    public bool TestClusterAccessForAccount(Cluster cluster, ClusterAuthenticationCredentials clusterAuthCredentials)
    {
        try
        {
            var schedulerConnection = _connectionPool.GetConnectionForUser(clusterAuthCredentials, cluster);
            _connectionPool.ReturnConnection(schedulerConnection);
            return true;
        }
        catch (Exception ex)
        {
            _log.Info(
                $"Cluster access test failed for project {clusterAuthCredentials.ClusterProjectCredentials.First().ClusterProject.ProjectId} - {ex.Message}");
            return false;
        }
    }

    public bool MoveJobFiles(SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations)
    {
        var schedulerConnection =
            _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster);
        try
        {
            return _adapter.MoveJobFiles(schedulerConnection.Connection, jobInfo, sourceDestinations);
        }
        finally
        {
            _connectionPool.ReturnConnection(schedulerConnection);
        }
    }

    public ClusterProjectCredentialCheckLog CheckClusterProjectCredentialStatus(ClusterProjectCredential clusterProjectCredential)
    {
        var clusterProject = clusterProjectCredential.ClusterProject;
        var clusterAuthCredentials = clusterProjectCredential.ClusterAuthenticationCredentials;
        var checkTimestamp = DateTime.UtcNow;
        var checkLog = new ClusterProjectCredentialCheckLog()
        {
            ClusterProjectId = clusterProject.Id,
            ClusterAuthenticationCredentialsId = clusterAuthCredentials.Id,
            CheckTimestamp = checkTimestamp,
            VaultCredentialOk = false,
            ClusterConnectionOk = false,
            DryRunJobOk = false,
            ErrorMessage = "",
            CreatedAt = checkTimestamp
        };

        ConnectionInfo schedulerConnection = null;
        try
        {
            schedulerConnection = _connectionPool.GetConnectionForUser(clusterAuthCredentials, clusterProject.Cluster);
            checkLog.ClusterConnectionOk = true;
            _adapter.CheckClusterAuthenticationCredentialsStatus(schedulerConnection.Connection, clusterProjectCredential, checkLog);
        }
        catch (Exception e)
        {
            checkLog.ErrorMessage += e.Message + "\n";
        }
        finally
        {
            if (schedulerConnection != null)
                _connectionPool.ReturnConnection(schedulerConnection);
        }

        if (checkLog.ErrorMessage != null && checkLog.ErrorMessage.Length > 500)
            checkLog.ErrorMessage = checkLog.ErrorMessage[..500];

        return checkLog;
    }

    #endregion
}