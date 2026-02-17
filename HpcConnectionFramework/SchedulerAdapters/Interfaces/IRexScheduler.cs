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
        ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(IEnumerable<SubmittedTaskInfo> submitedTasksInfo,
        ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    void CancelJob(IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message,
        ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    ClusterNodeUsage GetCurrentClusterNodeUsage(ClusterNodeType nodeType, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    IEnumerable<string> GetAllocatedNodes(SubmittedTaskInfo taskInfo,string sshCaToken, string lexisToken);

    IEnumerable<string> GetParametersFromGenericUserScript(Cluster cluster,
        ClusterAuthenticationCredentials serviceCredentials, string userScriptPath, string sshCaToken, string lexisToken);

    void AllowDirectFileTransferAccessForUserToJob(string publicKey, SubmittedJobInfo jobInfo, string sshCaToken, string lexisToken);

    void RemoveDirectFileTransferAccessForUser(IEnumerable<string> publicKeys,
        ClusterAuthenticationCredentials credentials, Cluster cluster, Project project, string sshCaToken, string lexisToken);

    void CreateJobDirectory(SubmittedJobInfo jobInfo, string localBasePath, bool sharedAccountsPoolMode, string sshCaToken, string lexisToken);

    bool DeleteJobDirectory(SubmittedJobInfo jobInfo, string localBasePath, string sshCaToken, string lexisToken);

    void CopyJobDataToTemp(SubmittedJobInfo jobInfo, string localBasePath, string hash, string path, string sshCaToken, string lexisToken);

    void CopyJobDataFromTemp(SubmittedJobInfo jobInfo, string localBasePath, string hash, string sshCaToken, string lexisToken);

    void CreateTunnel(SubmittedTaskInfo taskInfo, string nodeHost, int nodePort, string sshCaToken, string lexisToken);

    void RemoveTunnel(SubmittedTaskInfo taskInfo, string sshCaToken, string lexisToken);

    IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost);

    bool InitializeClusterScriptDirectory(string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath,
        Cluster cluster, ClusterAuthenticationCredentials clusterAuthCredentials, bool isServiceAccount, string sshCaToken, string lexisToken);

    (bool, string) TestClusterAccessForAccount(Cluster cluster, ClusterAuthenticationCredentials clusterAuthCredentials, string sshCaToken, string lexisToken);
    bool MoveJobFiles(SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, string sshCaToken, string lexisToken);

    Task<ClusterProjectCredentialCheckLog> CheckClusterProjectCredentialStatus(ClusterProjectCredential clusterProjectCredential);
    DryRunJobInfo DryRunJob(DryRunJobSpecification dryRunJobSpecification, string contextSshCaToken, string lexisToken);
}