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
    IEnumerable<SubmittedTaskInfo> SubmitJob(JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials, string sshCaToken);

    IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(IEnumerable<SubmittedTaskInfo> submitedTasksInfo,
        ClusterAuthenticationCredentials credentials, string sshCaToken);

    void CancelJob(IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message,
        ClusterAuthenticationCredentials credentials, string sshCaToken);

    ClusterNodeUsage GetCurrentClusterNodeUsage(ClusterNodeType nodeType, ClusterAuthenticationCredentials credentials, string sshCaToken);

    IEnumerable<string> GetAllocatedNodes(SubmittedTaskInfo taskInfo,string sshCaToken);

    IEnumerable<string> GetParametersFromGenericUserScript(Cluster cluster,
        ClusterAuthenticationCredentials serviceCredentials, string userScriptPath, string sshCaToken);

    void AllowDirectFileTransferAccessForUserToJob(string publicKey, SubmittedJobInfo jobInfo, string sshCaToken);

    void RemoveDirectFileTransferAccessForUser(IEnumerable<string> publicKeys,
        ClusterAuthenticationCredentials credentials, Cluster cluster, Project project, string sshCaToken);

    void CreateJobDirectory(SubmittedJobInfo jobInfo, string localBasePath, bool sharedAccountsPoolMode, string sshCaToken);

    bool DeleteJobDirectory(SubmittedJobInfo jobInfo, string localBasePath, string sshCaToken);

    void CopyJobDataToTemp(SubmittedJobInfo jobInfo, string localBasePath, string hash, string path, string sshCaToken);

    void CopyJobDataFromTemp(SubmittedJobInfo jobInfo, string localBasePath, string hash, string sshCaToken);

    void CreateTunnel(SubmittedTaskInfo taskInfo, string nodeHost, int nodePort, string sshCaToken);

    void RemoveTunnel(SubmittedTaskInfo taskInfo, string sshCaToken);

    IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost);

    bool InitializeClusterScriptDirectory(string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath,
        Cluster cluster, ClusterAuthenticationCredentials clusterAuthCredentials, bool isServiceAccount, string sshCaToken);

    bool TestClusterAccessForAccount(Cluster cluster, ClusterAuthenticationCredentials clusterAuthCredentials, string sshCaToken);
    bool MoveJobFiles(SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, string sshCaToken);

    Task<ClusterProjectCredentialCheckLog> CheckClusterProjectCredentialStatus(ClusterProjectCredential clusterProjectCredential);
    DryRunJobInfo DryRunJob(DryRunJobSpecification dryRunJobSpecification, string contextSshCaToken);
}