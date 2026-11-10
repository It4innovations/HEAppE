using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;

/// <summary>
///     IResx scheduler
/// </summary>
public interface IRexScheduler
{
    Task<IEnumerable<SubmittedTaskInfo>> SubmitJobAsync(JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfoAsync(IEnumerable<SubmittedTaskInfo> submitedTasksInfo,
        ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    Task CancelJobAsync(IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message,
        ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    Task<ClusterNodeUsage> GetCurrentClusterNodeUsageAsync(ClusterNodeType nodeType, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    Task<IEnumerable<string>> GetAllocatedNodesAsync(SubmittedTaskInfo taskInfo,string sshCaToken, string lexisToken);

    Task<IEnumerable<string>> GetParametersFromGenericUserScriptAsync(Cluster cluster,
        ClusterAuthenticationCredentials serviceCredentials, string userScriptPath, string sshCaToken, string lexisToken);

    Task AllowDirectFileTransferAccessForUserToJobAsync(string publicKey, SubmittedJobInfo jobInfo, string sshCaToken, string lexisToken);

    Task RemoveDirectFileTransferAccessForUserAsync(IEnumerable<string> publicKeys,
        ClusterAuthenticationCredentials credentials, Cluster cluster, Project project, string sshCaToken, string lexisToken);

    Task CreateJobDirectoryAsync(SubmittedJobInfo jobInfo, string localBasePath, bool sharedAccountsPoolMode, string sshCaToken, string lexisToken);

    Task<bool> DeleteJobDirectoryAsync(SubmittedJobInfo jobInfo, string localBasePath, string sshCaToken, string lexisToken);

    Task CopyJobDataToTempAsync(SubmittedJobInfo jobInfo, string localBasePath, string hash, string path, string sshCaToken, string lexisToken);

    Task CopyJobDataFromTempAsync(SubmittedJobInfo jobInfo, string localBasePath, string hash, string sshCaToken, string lexisToken);

    Task CreateTunnelAsync(SubmittedTaskInfo taskInfo, string nodeHost, int nodePort, string sshCaToken, string lexisToken);

    Task RemoveTunnelAsync(SubmittedTaskInfo taskInfo, string sshCaToken, string lexisToken);

    IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost);

    Task<bool> InitializeClusterScriptDirectoryAsync(string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath,
        Cluster cluster, ClusterAuthenticationCredentials clusterAuthCredentials, bool isServiceAccount, string sshCaToken, string lexisToken);

    Task<(bool, string)> TestClusterAccessForAccountAsync(Cluster cluster, ClusterAuthenticationCredentials clusterAuthCredentials, string sshCaToken, string lexisToken);
    Task<bool> MoveJobFilesAsync(SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, bool sharedAccountsPoolMode, string sshCaToken, string lexisToken);

    Task<ClusterProjectCredentialCheckLog> CheckClusterProjectCredentialStatus(ClusterProjectCredential clusterProjectCredential);
    Task<DryRunJobInfo> DryRunJobAsync(DryRunJobSpecification dryRunJobSpecification, string contextSshCaToken, string lexisToken);

    Task<IEnumerable<SubmittedTaskInfo>> GetHistoricalTasksInfoAsync(List<SubmittedTaskInfo> missingTasks,
        ClusterAuthenticationCredentials account, string sshCaToken, string lexisToken);

    Task<string> GetMachineArchitectureAsync(Cluster cluster, string machineId, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    Task<string> GetMachineInfoAsync(Cluster cluster, string machineId, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    Task<string> GetQSchedulerVersionAsync(Cluster cluster, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    Task<string> GetMachineCalibrationAsync(Cluster cluster, string machineId, string calibrationId, string endpoint, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);
    Task<long> OpenSessionAsync(Cluster cluster, string machineId, string project, int walltimeLimitSecs, string ownerUsername, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);
    Task CloseSessionAsync(Cluster cluster, long sessionId, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    /// <summary>
    ///     Returns the QScheduler session JSON (GET /sessions/{id}), including its consumption (exectime_ms).
    /// </summary>
    Task<string> GetSessionInfoAsync(Cluster cluster, long sessionId, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    /// <summary>
    ///     Lists all QScheduler projects (GET /projects).
    /// </summary>
    Task<System.Collections.Generic.IList<QScheduler.DTO.QSchedulerProjectInfo>> ListQSchedulerProjectsAsync(Cluster cluster, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    /// <summary>
    ///     Updates a QScheduler project's limit and/or active flag; null leaves a value unchanged.
    /// </summary>
    Task UpdateQSchedulerProjectAsync(Cluster cluster, string qSchedulerProjectName, long? limitMs, bool? active, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);
    Task<System.IO.Stream> GetQuantumTaskResultAsync(Cluster cluster, string scheduledJobId, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);
    Task<System.IO.Stream> GetQuantumTaskArtifactAsync(Cluster cluster, string scheduledJobId, string artifactName, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);
    Task<MachineArtifact> GetMachineArtifactAsync(Cluster cluster, string machineId, string artifactName, string accept, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);
}