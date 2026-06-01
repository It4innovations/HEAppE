using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;

/// <summary>
///     IScheduler adapter
/// </summary>
public interface ISchedulerAdapter
{
    Task<IEnumerable<SubmittedTaskInfo>> SubmitJob(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials);

    Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfo(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string key);

    Task CancelJob(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message);

    Task<ClusterNodeUsage> GetCurrentClusterNodeUsage(object connectorClient, ClusterNodeType nodeType);

    Task<IEnumerable<string>> GetAllocatedNodes(object connectorClient, SubmittedTaskInfo taskInfo);

    Task<IEnumerable<string>> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath);

    Task AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo);

    Task RemoveDirectFileTransferAccessForUser(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString);

    Task CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode);

    Task<bool> DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath);

    Task CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash,
        string path);

    Task CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash, string localBasePath);

    Task CreateTunnel(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort);

    Task RemoveTunnel(object connectorClient, SubmittedTaskInfo taskInfo);

    IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost);

    Task<bool> InitializeClusterScriptDirectory(object schedulerConnectionConnection, string clusterProjectRootDirectory,
        bool overwriteExistingProjectRootDirectory, string localBasepath, string account, bool isServiceAccount);

    Task<bool> MoveJobFiles(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, bool sharedAccountsPoolMode);

    Task<dynamic> CheckClusterAuthenticationCredentialsStatus(object connectorClient, ClusterProjectCredential clusterProjectCredential, ClusterProjectCredentialCheckLog checkLog);
    
    Task<DryRunJobInfo> DryRunJob(object schedulerConnectionConnection, DryRunJobSpecification dryRunJobSpecification);
    
    Task<IEnumerable<SubmittedTaskInfo>> GetHistoricalTasksInfo(object schedulerConnectionConnection, List<SubmittedTaskInfo> missingTasks, ClusterAuthenticationCredentials account);
}