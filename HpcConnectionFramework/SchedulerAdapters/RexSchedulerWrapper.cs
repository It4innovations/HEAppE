using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
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

    /// <summary>
    ///     Submit job to scheduler
    /// </summary>
    /// <param name="jobSpecification">Job specification</param>
    /// <param name="credentials">Credentials</param>
    /// <returns></returns>
    public async Task<IEnumerable<SubmittedTaskInfo>> SubmitJobAsync(JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken)
    {
        var schedulerConnection =  await GetConnectionForUser(
            credentials, jobSpecification.Cluster, sshCaToken, lexisToken);
        try
        {
            var tasks = await _adapter.SubmitJob(schedulerConnection?.Connection, jobSpecification, credentials);
            return tasks;
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
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

        var schedulerConnection = await GetConnectionForUser(credentials, cluster, sshCaToken, lexisToken);
        try
        {
            foreach (var groupedTasksByUsername in groupedTasksByUser)
            {
                    var tasks = await _adapter.GetActualTasksInfo(schedulerConnection?.Connection, cluster,
                        groupedTasksByUsername.ToList(), groupedTasksByUsername.Key);
                allTasks.AddRange(tasks);
            }

            return allTasks;
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
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
        var schedulerConnection = await GetConnectionForUser(credentials, cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.CancelJob(schedulerConnection?.Connection, submitedTasksInfo, message);
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
        }
    }

    /// <summary>
    ///     Get actual scheduler queue status
    /// </summary>
    /// <param name="nodeType">Cluster node type</param>
    public async Task<ClusterNodeUsage> GetCurrentClusterNodeUsageAsync(ClusterNodeType nodeType,
        ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken)
    {
        var schedulerConnection = await GetConnectionForUser(credentials, nodeType.Cluster, sshCaToken, lexisToken);
        try
        {
            var usage = await _adapter.GetCurrentClusterNodeUsage(schedulerConnection.Connection, nodeType);
            return usage;
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
        }
    }

    /// <summary>
    ///     Get allocated nodes address for task
    /// </summary>
    /// <param name="taskInfo">Task information</param>
    public async Task<IEnumerable<string>> GetAllocatedNodesAsync(SubmittedTaskInfo taskInfo, string sshCaToken, string lexisToken)
    {
        var cluster = taskInfo.Specification.JobSpecification.Cluster;
        var schedulerConnection = await GetConnectionForUser(
            taskInfo.Specification.JobSpecification.ClusterUser, taskInfo.Specification.JobSpecification.Cluster, sshCaToken, lexisToken);
        try
        {
            return await _adapter.GetAllocatedNodes(schedulerConnection?.Connection, taskInfo);
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
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
        var schedulerConnection = await GetConnectionForUser(serviceCredentials, cluster, sshCaToken, lexisToken);
        try
        {
            return await _adapter.GetParametersFromGenericUserScript(schedulerConnection?.Connection, userScriptPath);
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
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
            await GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.AllowDirectFileTransferAccessForUserToJob(schedulerConnection?.Connection, publicKey, jobInfo);
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
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
        var schedulerConnection = await GetConnectionForUser(credentials, cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.RemoveDirectFileTransferAccessForUser(schedulerConnection?.Connection, publicKeys,
                project.AccountingString);
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
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
        var cluster = jobInfo.Specification.Cluster;

        if (cluster.SchedulerType.HasFlag(SchedulerType.FirecRest))
        {
            await _adapter.CreateJobDirectory(null, jobInfo, localBasePath, sharedAccountsPoolMode);
            return;
        }
        
        var schedulerConnection = await GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        try
        {
            var localBasepath = jobInfo.Specification.Cluster.ClusterProjects.Find(cp => cp.ProjectId == jobInfo.Specification.ProjectId)
                ?.ScratchStoragePath;
            string path = Path.Combine(jobInfo.Specification.Project.AccountingString, HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath); 
    
            bool isUpdated = await _adapter.InitializeClusterScriptDirectory(schedulerConnection.Connection, path, true, localBasepath,
                jobInfo.Specification.ClusterUser.Username, false);
            if (!isUpdated)
            {
                _logger.LogWarning($"Cluster script directory updated failed for project {jobInfo.Specification.Project.Id} for user {jobInfo.Specification.ClusterUser.Username} before job submission.");
            }
            else
            {
                _logger.LogInformation($"Cluster script directory updated for project {jobInfo.Specification.Project.Id} for user {jobInfo.Specification.ClusterUser.Username} before job submission.");
            }
            await _adapter.CreateJobDirectory(schedulerConnection.Connection, jobInfo, localBasePath, sharedAccountsPoolMode);
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
        }
    }

    /// <summary>
    ///     Delete job directory
    /// </summary>
    /// <param name="jobInfo">Job info</param>
    public async Task<bool> DeleteJobDirectoryAsync(SubmittedJobInfo jobInfo, string localBasePath, string sshCaToken, string lexisToken)
    {
        var schedulerConnection = await GetConnectionForUser(
            jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        try
        {
            return await _adapter.DeleteJobDirectory(schedulerConnection?.Connection, jobInfo, localBasePath);
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
            await ReturnConnection(schedulerConnection);
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
            await GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.CopyJobDataToTemp(schedulerConnection?.Connection, jobInfo, localBasePath, hash, path);
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
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
        var schedulerConnection = await GetConnectionForUser(
            jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.CopyJobDataFromTemp(schedulerConnection?.Connection, jobInfo, localBasePath, hash);
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
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
        var schedulerConnection = await GetConnectionForUser(
            taskInfo.Specification.JobSpecification.ClusterUser, taskInfo.Specification.JobSpecification.Cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.CreateTunnel(schedulerConnection?.Connection, taskInfo, nodeHost, nodePort);
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
        }
    }

    /// <summary>
    ///     Remove tunnel
    /// </summary>
    /// <param name="taskInfo">Task info</param>
    public async Task RemoveTunnelAsync(SubmittedTaskInfo taskInfo, string sshCaToken, string lexisToken)
    {
        var cluster = taskInfo.Specification.JobSpecification.Cluster;
        var schedulerConnection = await GetConnectionForUser(
            taskInfo.Specification.JobSpecification.ClusterUser, taskInfo.Specification.JobSpecification.Cluster, sshCaToken, lexisToken);
        try
        {
            await _adapter.RemoveTunnel(schedulerConnection?.Connection, taskInfo);
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
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
            schedulerConnection = await GetConnectionForUser(clusterAuthCredentials, cluster, sshCaToken, lexisToken);
            return await _adapter.InitializeClusterScriptDirectory(schedulerConnection?.Connection,
                clusterProjectRootDirectory, overwriteExistingProjectRootDirectory, localBasepath,
                clusterAuthCredentials.Username, isServiceAccount);
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
            await ReturnConnection(schedulerConnection);
        }
    }

    public async Task<(bool,string)> TestClusterAccessForAccountAsync(Cluster cluster, ClusterAuthenticationCredentials clusterAuthCredentials, string sshCaToken, string lexisToken)
    {
        try
        {
            var schedulerConnection = await GetConnectionForUser(clusterAuthCredentials, cluster, sshCaToken, lexisToken);
            await ReturnConnection(schedulerConnection);
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
        var schedulerConnection = await GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        try
        {
            return await _adapter.MoveJobFiles(schedulerConnection?.Connection, jobInfo, sourceDestinations, sharedAccountsPoolMode);
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
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
            schedulerConnection = await GetConnectionForUser(clusterAuthCredentials, cluster, null, null);
            checkLog.ClusterConnectionOk = true;
            await _adapter.CheckClusterAuthenticationCredentialsStatus(schedulerConnection?.Connection, clusterProjectCredential, checkLog);
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
            await ReturnConnection(schedulerConnection);
        }

        if (checkLog.ErrorMessage != null && checkLog.ErrorMessage.Length > 500)
            checkLog.ErrorMessage = checkLog.ErrorMessage[..500];

        return checkLog;
    }

    public async Task<DryRunJobInfo> DryRunJobAsync(DryRunJobSpecification dryRunJobSpecification, string contextSshCaToken, string lexisToken)
    {
        var schedulerConnection = await GetConnectionForUser(dryRunJobSpecification.ClusterUser, dryRunJobSpecification.ClusterNodeType.Cluster, contextSshCaToken, lexisToken);
        try
        {
            return await _adapter.DryRunJob(schedulerConnection.Connection, dryRunJobSpecification);
        }
        finally
        {
            await ReturnConnection(schedulerConnection);
        }
    }

    private async Task<ConnectionInfo> GetConnectionForUser(ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken)
    {
        if (_connectionPool != null && !cluster.SchedulerType.HasFlag(SchedulerType.FirecRest))
            return await _connectionPool.GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
        return null;
    }

    private Task ReturnConnection(ConnectionInfo schedulerConnection)
    {
        if (_connectionPool == null || schedulerConnection == null)
            return Task.Delay(1);
        return _connectionPool.ReturnConnectionAsync(schedulerConnection);
    }

    #endregion
}