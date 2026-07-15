using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using Microsoft.Extensions.Logging;
using Renci.SshNet.Common;
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
    public RexSchedulerWrapper(IConnectionPool connectionPool, ISchedulerAdapter adapter, ILogger logger)
    {
        _logger = logger;
        _connectionPool = connectionPool;
        _adapter = adapter;
    }

    #endregion

    #region Instances

    public Project Project { get; set; }

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
    protected ILogger _logger;

    #endregion

    #region IRexScheduler Members

    public async Task<IEnumerable<SubmittedTaskInfo>> SubmitJobAsync(JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken)
    {
        var schedulerConnection =  await GetConnectionForUserAsync(
            credentials, jobSpecification.Cluster, sshCaToken, lexisToken);
        try
        {
            var tasks = await _adapter.SubmitJobAsync(schedulerConnection.Connection, jobSpecification, credentials);
            return tasks;
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }
    
    /// <summary>
    ///     Get actual tasks
    /// </summary>
    /// <param name="submitedTasksInfo">Submitted tasks ids</param>
    /// <param name="credentials">Credentials</param>
    /// <returns></returns>
    public async Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfoAsync(IEnumerable<SubmittedTaskInfo> submitedTasksInfo,
        ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken)
    {
        if (submitedTasksInfo == null || !submitedTasksInfo.Any())
        {
            return Enumerable.Empty<SubmittedTaskInfo>();
        }

        var cluster = submitedTasksInfo.First().Specification.JobSpecification.Cluster;
        var allTasks = new List<SubmittedTaskInfo>();
        var groupedTasksByUser = submitedTasksInfo
            .GroupBy(t => t.Specification.JobSpecification.ClusterUser.Username);

        var schedulerConnection = await GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
        try
        {
            foreach (var groupedTasksByUsername in groupedTasksByUser)
            {
                var tasks = await _adapter.GetActualTasksInfoAsync(schedulerConnection.Connection, cluster,
                        groupedTasksByUsername.ToList(), groupedTasksByUsername.Key);
                allTasks.AddRange(tasks);
            }

            return allTasks;
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    /// <summary>
    ///     Cancel job
    /// </summary>
    /// <param name="submitedTasksInfo">Submitted tasks id´s</param>
    /// <param name="message">Message</param>
    /// <param name="credentials">Credentials</param>
    public async Task CancelJobAsync(IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message,
        ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken)
    {
        var cluster = submitedTasksInfo.FirstOrDefault().Specification.JobSpecification.Cluster;
        var schedulerConnection = await GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.CancelJobAsync(schedulerConnection.Connection, submitedTasksInfo, message);
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    /// <summary>
    ///     Get actual scheduler queue status
    /// </summary>
    /// <param name="nodeType">Cluster node type</param>
    public async Task<ClusterNodeUsage> GetCurrentClusterNodeUsageAsync(ClusterNodeType nodeType,
        ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken)
    {
        var schedulerConnection = await GetConnectionForUserAsync(credentials, nodeType.Cluster, sshCaToken, lexisToken);
        try
        {
            var usage = await _adapter.GetCurrentClusterNodeUsageAsync(schedulerConnection.Connection, nodeType);
            return usage;
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    /// <summary>
    ///     Get allocated nodes address for task
    /// </summary>
    /// <param name="taskInfo">Task information</param>
    public async Task<IEnumerable<string>> GetAllocatedNodesAsync(SubmittedTaskInfo taskInfo, string sshCaToken, string lexisToken)
    {
        var cluster = taskInfo.Specification.JobSpecification.Cluster;
        var schedulerConnection = await GetConnectionForUserAsync(
            taskInfo.Specification.JobSpecification.ClusterUser, taskInfo.Specification.JobSpecification.Cluster, sshCaToken, lexisToken);
        try
        {
            return await _adapter.GetAllocatedNodesAsync(schedulerConnection.Connection, taskInfo);
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    /// <summary>
    ///     Get generic command templates parameters from script
    /// </summary>
    /// <param name="cluster">Cluster</param>
    /// <param name="userScriptPath">Generic script path</param>
    /// <returns></returns>
    public async Task<IEnumerable<string>> GetParametersFromGenericUserScriptAsync(Cluster cluster,
        ClusterAuthenticationCredentials serviceCredentials, string userScriptPath, string sshCaToken, string lexisToken)
    {
        var schedulerConnection = await GetConnectionForUserAsync(serviceCredentials, cluster, sshCaToken, lexisToken);
        try
        {
            return await _adapter.GetParametersFromGenericUserScriptAsync(schedulerConnection.Connection, userScriptPath);
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    /// <summary>
    ///     Allow direct file transfer acces for user
    /// </summary>
    /// <param name="publicKey">Public key</param>
    /// <param name="jobInfo">Job info</param>
    public async Task AllowDirectFileTransferAccessForUserToJobAsync(string publicKey, SubmittedJobInfo jobInfo, string sshCaToken, string lexisToken)
    {
        var cluster = jobInfo.Specification.Cluster;
        var schedulerConnection = 
            await GetConnectionForUserAsync(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.AllowDirectFileTransferAccessForUserToJobAsync(schedulerConnection.Connection, publicKey, jobInfo);
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    /// <summary>
    ///     Remove direct file transfer access for user
    /// </summary>
    /// <param name="publicKeys">Public keys</param>
    /// <param name="credentials">Credentials</param>
    public async Task RemoveDirectFileTransferAccessForUserAsync(IEnumerable<string> publicKeys,
        ClusterAuthenticationCredentials credentials, Cluster cluster, Project project, string sshCaToken, string lexisToken)
    {
        var schedulerConnection = await GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.RemoveDirectFileTransferAccessForUserAsync(schedulerConnection.Connection, publicKeys,
                project.AccountingString);
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }
    
    // In-flight deduplication for InitializeClusterScriptDirectory SSH calls.
    // Key: (credentialId, clusterProjectPath). Value: the currently running SSH init Task.
    // IMPORTANT: callers await this BEFORE acquiring a connection from the pool, so only
    // the "winner" holds a connection during the git pull. All others wait connection-free.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(long, string), Task<bool>> _scriptInitInFlight
        = new();

    private async Task<bool> RunScriptInitOnDedicatedConnectionAsync(
        ClusterAuthenticationCredentials credentials, Cluster cluster,
        string path, string localBasepath, string sshCaToken, string lexisToken)
    {
        var conn = await GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
        try
        {
            return await _adapter.InitializeClusterScriptDirectoryAsync(
                conn.Connection, path, false, localBasepath, credentials.Username, false, cluster.CustomConfiguration);
        }
        finally
        {
            await ReturnConnectionAsync(conn);
        }
    }

    /// <summary>
    ///     Create job directory
    /// </summary>
    /// <param name="jobInfo">Job info</param>
    /// <param name="localBasePath"></param>
    /// <param name="sharedAccountsPoolMode"></param>
    public async Task CreateJobDirectoryAsync(SubmittedJobInfo jobInfo, string localBasePath, bool sharedAccountsPoolMode, string sshCaToken, string lexisToken)
    {
        var localBasepath = jobInfo.Specification.Cluster.ClusterProjects
            .Find(cp => cp.ProjectId == jobInfo.Specification.ProjectId)?.ScratchStoragePath;
        var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        string path = Path.Combine(jobInfo.Specification.Project.AccountingString,
            clusterConfig.InstanceIdentifierPath);

        var cacheKey = (jobInfo.Specification.ClusterUser.Id, path);

        // Step 1: Await script init WITHOUT holding a connection.
        // Only the "winner" VU acquires a connection for git pull; the rest wait for free.
        // ContinueWith removes the entry so the next logical burst always runs a fresh git pull.
        var initTask = _scriptInitInFlight.GetOrAdd(cacheKey, _ =>
        {
            _logger.LogDebug($"Starting InitializeClusterScriptDirectory for project {jobInfo.Specification.Project.Id}.");
            var t = RunScriptInitOnDedicatedConnectionAsync(
                jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster,
                path, localBasepath, sshCaToken, lexisToken);
            t.ContinueWith(_ => _scriptInitInFlight.TryRemove(cacheKey, out _),
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously);
            return t;
        });

        bool isUpdated = await initTask;
        if (!isUpdated)
            _logger.LogWarning($"Cluster script directory update failed for project {jobInfo.Specification.Project.Id} user {jobInfo.Specification.ClusterUser.Username}.");
        else
            _logger.LogInformation($"Cluster script directory updated for project {jobInfo.Specification.Project.Id} user {jobInfo.Specification.ClusterUser.Username}.");

        // Step 2: NOW acquire a connection — only for the fast CreateJobDirectory mkdir.
        var schedulerConnection = await GetConnectionForUserAsync(
            jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.CreateJobDirectoryAsync(schedulerConnection.Connection, jobInfo, localBasePath, sharedAccountsPoolMode);
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    /// <summary>
    ///     Delete job directory
    /// </summary>
    /// <param name="jobInfo">Job info</param>
    public async Task<bool> DeleteJobDirectoryAsync(SubmittedJobInfo jobInfo, string localBasePath, string sshCaToken, string lexisToken)
    {
        var schedulerConnection = await GetConnectionForUserAsync(
            jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        try
        {
            return await _adapter.DeleteJobDirectoryAsync(schedulerConnection.Connection, jobInfo, localBasePath);
        }
        catch (HEAppE.Exceptions.AbstractTypes.BaseException)
        {
            throw;
        }
        catch (SshException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting job directory for job {jobInfo.Id}");
            return false;
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    /// <summary>
    ///     Copy job data to temp folder
    /// </summary>
    /// <param name="jobInfo">Job info</param>
    /// <param name="hash">Hash</param>
    /// <param name="path">Path</param>
    public async Task CopyJobDataToTempAsync(SubmittedJobInfo jobInfo, string localBasePath, string hash, string path, string sshCaToken, string lexisToken)
    {
        var cluster = jobInfo.Specification.Cluster;
        var schedulerConnection = 
            await GetConnectionForUserAsync(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.CopyJobDataToTempAsync(schedulerConnection.Connection, jobInfo, localBasePath, hash, path);
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    /// <summary>
    ///     Copy job data from temp folder
    /// </summary>
    /// <param name="jobInfo">Job info</param>
    /// <param name="hash">Hash</param>
    public async Task CopyJobDataFromTempAsync(SubmittedJobInfo jobInfo, string localBasePath, string hash, string sshCaToken, string lexisToken)
    {
        var cluster = jobInfo.Specification.Cluster;
        var schedulerConnection = await GetConnectionForUserAsync(
            jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.CopyJobDataFromTempAsync(schedulerConnection.Connection, jobInfo, localBasePath, hash);
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    /// <summary>
    ///     Create tunnel
    /// </summary>
    /// <param name="taskInfo">Task info</param>
    /// <param name="nodeHost">Cluster node address</param>
    /// <param name="nodePort">Cluster node port</param>
    public async Task CreateTunnelAsync(SubmittedTaskInfo taskInfo, string nodeHost, int nodePort, string sshCaToken, string lexisToken)
    {
        var cluster = taskInfo.Specification.JobSpecification.Cluster;
        var schedulerConnection = await GetConnectionForUserAsync(
            taskInfo.Specification.JobSpecification.ClusterUser, taskInfo.Specification.JobSpecification.Cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.CreateTunnelAsync(schedulerConnection.Connection, taskInfo, nodeHost, nodePort);
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    /// <summary>
    ///     Remove tunnel
    /// </summary>
    /// <param name="taskInfo">Task info</param>
    public async Task RemoveTunnelAsync(SubmittedTaskInfo taskInfo, string sshCaToken, string lexisToken)
    {
        var cluster = taskInfo.Specification.JobSpecification.Cluster;
        var schedulerConnection = await GetConnectionForUserAsync(
            taskInfo.Specification.JobSpecification.ClusterUser, taskInfo.Specification.JobSpecification.Cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.RemoveTunnelAsync(schedulerConnection.Connection, taskInfo);
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
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
    public async Task<bool> InitializeClusterScriptDirectoryAsync(string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath,
        Cluster cluster, ClusterAuthenticationCredentials clusterAuthCredentials, bool isServiceAccount, string sshCaToken, string lexisToken)
    {
        ConnectionInfo schedulerConnection = null;
        try
        {
            schedulerConnection = await GetConnectionForUserAsync(clusterAuthCredentials, cluster, sshCaToken, lexisToken);
            var customConfig = new Dictionary<string, string>(cluster.CustomConfiguration ?? new Dictionary<string, string>());
            if (Project != null)
            {
                customConfig["ProjectAccountingString"] = Project.AccountingString;
                if (Project.UsageType == HEAppE.DomainObjects.JobReporting.Enums.UsageType.QPUSeconds)
                {
                    var aggregations = Project.ProjectClusterNodeTypeAggregations?
                        .Where(a => a.ClusterNodeTypeAggregation != null)
                        .ToList();
                    if (aggregations != null && aggregations.Any())
                    {
                        var totalAllocation = aggregations.Sum(a => a.AllocationAmount);
                        customConfig["ProjectQuantumSecondsLimit"] = totalAllocation.ToString();
                    }
                }
            }
            return await _adapter.InitializeClusterScriptDirectoryAsync(schedulerConnection.Connection,
                clusterProjectRootDirectory, overwriteExistingProjectRootDirectory, localBasepath,
                clusterAuthCredentials.Username, isServiceAccount, customConfig);
        }
        catch (HEAppE.Exceptions.AbstractTypes.BaseException)
        {
            throw;
        }
        catch (SshException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Cluster script directory initialization failed for project {clusterAuthCredentials.ClusterProjectCredentials.First().ClusterProject.ProjectId}, {ex.Message}");
            return false;
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    public async Task<(bool,string)> TestClusterAccessForAccountAsync(Cluster cluster, ClusterAuthenticationCredentials clusterAuthCredentials, string sshCaToken, string lexisToken)
    {
        try
        {
            var schedulerConnection = await GetConnectionForUserAsync(clusterAuthCredentials, cluster, sshCaToken, lexisToken);
            await ReturnConnectionAsync(schedulerConnection);
            return (true, "Cluster access test successful");
        }
        catch (HEAppE.Exceptions.AbstractTypes.BaseException)
        {
            throw;
        }
        catch (SshException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Cluster access test failed for project {clusterAuthCredentials.ClusterProjectCredentials.First().ClusterProject.ProjectId} - {ex.Message}");
            return (false, $"Cluster access test failed for project {clusterAuthCredentials.ClusterProjectCredentials.First().ClusterProject.ProjectId} - {ex.Message}");
        }
    }

    public async Task<bool> MoveJobFilesAsync(SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, bool sharedAccountsPoolMode, string sshCaToken, string lexisToken)
    {
        var cluster = jobInfo.Specification.Cluster;
        var schedulerConnection = await GetConnectionForUserAsync(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        try
        {
            return await _adapter.MoveJobFilesAsync(schedulerConnection.Connection, jobInfo, sourceDestinations, sharedAccountsPoolMode);
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    public async Task<ClusterProjectCredentialCheckLog> CheckClusterProjectCredentialStatus(ClusterProjectCredential clusterProjectCredential)
    {
        ClusterProject clusterProject = clusterProjectCredential.ClusterProject;
        ClusterAuthenticationCredentials clusterAuthCredentials = clusterProjectCredential.ClusterAuthenticationCredentials;
        Cluster cluster = clusterProject.Cluster;

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
            schedulerConnection = await GetConnectionForUserAsync(clusterAuthCredentials, cluster, null, null);
            checkLog.ClusterConnectionOk = true;
            await _adapter.CheckClusterAuthenticationCredentialsStatus(schedulerConnection.Connection, clusterProjectCredential, checkLog);
        }
        catch (HEAppE.Exceptions.AbstractTypes.BaseException)
        {
            throw;
        }
        catch (SshException)
        {
            throw;
        }
        catch (Exception e)
        {
            checkLog.ErrorMessage += e.Message + "\n";
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }

        if (checkLog.ErrorMessage != null && checkLog.ErrorMessage.Length > 500)
            checkLog.ErrorMessage = checkLog.ErrorMessage[..500];

        return checkLog;
    }

    public async Task<DryRunJobInfo> DryRunJobAsync(DryRunJobSpecification dryRunJobSpecification, string contextSshCaToken, string lexisToken)
    {
        var schedulerConnection = await GetConnectionForUserAsync(dryRunJobSpecification.ClusterUser, dryRunJobSpecification.ClusterNodeType.Cluster, contextSshCaToken, lexisToken);
        try
        {
            return await _adapter.DryRunJobAsync(schedulerConnection.Connection, dryRunJobSpecification);
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }
    
    public async Task<IEnumerable<SubmittedTaskInfo>> GetHistoricalTasksInfoAsync(
        List<SubmittedTaskInfo> missingTasks, 
        ClusterAuthenticationCredentials account, 
        string sshCaToken, 
        string lexisToken)
    {
        if (missingTasks == null || !missingTasks.Any())
        {
            return Enumerable.Empty<SubmittedTaskInfo>();
        }

        var cluster = missingTasks.FirstOrDefault()?.Specification?.JobSpecification?.Cluster;
        if (cluster == null)
        {
            _logger.LogWarning("Cannot retrieve historical tasks info: Cluster context is missing in task specifications.");
            return Enumerable.Empty<SubmittedTaskInfo>();
        }

        var schedulerConnection = await GetConnectionForUserAsync(account, cluster, sshCaToken, lexisToken);
        try
        {
            var historicalTasks = await _adapter.GetHistoricalTasksInfoAsync(schedulerConnection.Connection, missingTasks, account);
            return historicalTasks;
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    public async Task<string> GetMachineArchitectureAsync(Cluster cluster, string machineId, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken)
    {
        var schedulerConnection = await GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
        try
        {
            return await _adapter.GetMachineArchitectureAsync(schedulerConnection.Connection, cluster, machineId);
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }

    public async Task<string> GetMachineCalibrationAsync(Cluster cluster, string machineId, string calibrationId, string endpoint, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken)
    {
        var schedulerConnection = await GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
        try
        {
            return await _adapter.GetMachineCalibrationAsync(schedulerConnection.Connection, cluster, machineId, calibrationId, endpoint);
        }
        finally
        {
            await ReturnConnectionAsync(schedulerConnection);
        }
    }
    public async Task<long> OpenSessionAsync(Cluster cluster, string machineId, string project, int walltimeLimitSecs, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken)
    {
        if (_adapter is QScheduler.Generic.QSchedulerSchedulerAdapter qScheduler)
        {
            var schedulerConnection = await GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
            try
            {
                return await qScheduler.OpenSessionAsync(schedulerConnection.Connection, cluster, machineId, this.Project, walltimeLimitSecs);
            }
            finally
            {
                await ReturnConnectionAsync(schedulerConnection);
            }
        }
        throw new NotSupportedException("OpenSession is only supported by QScheduler.");
    }

    public async Task CloseSessionAsync(Cluster cluster, long sessionId, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken)
    {
        if (_adapter is QScheduler.Generic.QSchedulerSchedulerAdapter qScheduler)
        {
            var schedulerConnection = await GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
            try
            {
                await qScheduler.CloseSessionAsync(schedulerConnection.Connection, cluster, sessionId);
            }
            finally
            {
                await ReturnConnectionAsync(schedulerConnection);
            }
        }
        else
        {
            throw new NotSupportedException("CloseSession is only supported by QScheduler.");
        }
    }

    public async Task<System.IO.Stream> GetQuantumTaskResultAsync(Cluster cluster, string scheduledJobId, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken)
    {
        if (_adapter is QScheduler.Generic.QSchedulerSchedulerAdapter qScheduler)
        {
            var schedulerConnection = await GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
            try
            {
                return await qScheduler.GetQuantumTaskResultAsync(schedulerConnection.Connection, cluster, scheduledJobId);
            }
            finally
            {
                await ReturnConnectionAsync(schedulerConnection);
            }
        }
        else
        {
            throw new NotSupportedException("GetQuantumTaskResult is only supported by QScheduler.");
        }
    }

    public async Task<System.IO.Stream> GetQuantumTaskArtifactAsync(Cluster cluster, string scheduledJobId, string artifactName, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken)
    {
        if (_adapter is QScheduler.Generic.QSchedulerSchedulerAdapter qScheduler)
        {
            var schedulerConnection = await GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
            try
            {
                return await qScheduler.GetQuantumTaskArtifactAsync(schedulerConnection.Connection, cluster, scheduledJobId, artifactName);
            }
            finally
            {
                await ReturnConnectionAsync(schedulerConnection);
            }
        }
        else
        {
            throw new NotSupportedException("GetQuantumTaskArtifact is only supported by QScheduler.");
        }
    }

    private readonly ConnectionInfo DummyConnectionInfo = new ConnectionInfo { Connection = new object(), AuthCredentials = null, LastUsed = DateTime.Now };

    private async Task<ConnectionInfo> GetConnectionForUserAsync(ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken)
    {
        if (_connectionPool == null || cluster.SchedulerType.HasFlag(SchedulerType.FirecRestSlurm))
            return DummyConnectionInfo;
        return await _connectionPool.GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
    }

    private Task ReturnConnectionAsync(ConnectionInfo schedulerConnection)
    {
        if (_connectionPool == null || schedulerConnection == null || schedulerConnection == DummyConnectionInfo)
            return Task.Delay(1);
        return _connectionPool.ReturnConnectionAsync(schedulerConnection);
    }

    #endregion
}