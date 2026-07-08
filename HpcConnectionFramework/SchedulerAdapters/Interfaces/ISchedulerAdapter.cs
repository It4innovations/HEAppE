using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using Cluster = HEAppE.DomainObjects.ClusterInformation.Cluster;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;

/// <summary>
///     IScheduler adapter
/// </summary>
public interface ISchedulerAdapter
{
    Task<IEnumerable<SubmittedTaskInfo>> SubmitJobAsync(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials);

    Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfoAsync(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string key);

    Task CancelJobAsync(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message);

    Task<ClusterNodeUsage> GetCurrentClusterNodeUsageAsync(object connectorClient, ClusterNodeType nodeType);

    Task<IEnumerable<string>> GetAllocatedNodesAsync(object connectorClient, SubmittedTaskInfo taskInfo);

    Task<IEnumerable<string>> GetParametersFromGenericUserScriptAsync(object connectorClient, string userScriptPath);

    Task AllowDirectFileTransferAccessForUserToJobAsync(object connectorClient, string publicKey, SubmittedJobInfo jobInfo);

    Task RemoveDirectFileTransferAccessForUserAsync(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString);

    Task CreateJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode);

    Task<bool> DeleteJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath);

    Task CopyJobDataToTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash,
        string path);

    Task CopyJobDataFromTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string hash, string localBasePath);

    Task CreateTunnelAsync(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort);

    Task RemoveTunnelAsync(object connectorClient, SubmittedTaskInfo taskInfo);

    IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost);

    Task<bool> InitializeClusterScriptDirectoryAsync(object schedulerConnectionConnection, string clusterProjectRootDirectory,
        bool overwriteExistingProjectRootDirectory, string localBasepath, string account, bool isServiceAccount, Dictionary<string, string>? customConfiguration);

    Task<bool> MoveJobFilesAsync(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, bool sharedAccountsPoolMode);

    Task<dynamic> CheckClusterAuthenticationCredentialsStatus(object connectorClient, ClusterProjectCredential clusterProjectCredential, ClusterProjectCredentialCheckLog checkLog);
    
    Task<DryRunJobInfo> DryRunJobAsync(object schedulerConnectionConnection, DryRunJobSpecification dryRunJobSpecification);
    
    Task<IEnumerable<SubmittedTaskInfo>> GetHistoricalTasksInfoAsync(object schedulerConnectionConnection, List<SubmittedTaskInfo> missingTasks, ClusterAuthenticationCredentials account);

    Task<string> GetMachineArchitectureAsync(object connectorClient, Cluster cluster, int machineId);

    Task<string> GetMachineCalibrationAsync(object connectorClient, Cluster cluster, int machineId, string calibrationId, string endpoint);
}