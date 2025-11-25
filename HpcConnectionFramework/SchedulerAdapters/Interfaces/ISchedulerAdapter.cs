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
    IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials);

    IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string key);

    void CancelJob(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message);

    ClusterNodeUsage GetCurrentClusterNodeUsage(object connectorClient, ClusterNodeType nodeType);

    IEnumerable<string> GetAllocatedNodes(object connectorClient, SubmittedTaskInfo taskInfo);

    IEnumerable<string> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath);

    void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo);

    void RemoveDirectFileTransferAccessForUser(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString);

    void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode);

    bool DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath);

    void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash,
        string path);

    void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash, string localBasePath);

    void CreateTunnel(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort);

    void RemoveTunnel(object connectorClient, SubmittedTaskInfo taskInfo);

    IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost);

    bool InitializeClusterScriptDirectory(object schedulerConnectionConnection, string clusterProjectRootDirectory,
        bool overwriteExistingProjectRootDirectory, string localBasepath, string account, bool isServiceAccount);

    bool MoveJobFiles(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations);

    Task<dynamic> CheckClusterAuthenticationCredentialsStatus(object connectorClient, ClusterProjectCredential clusterProjectCredential, ClusterProjectCredentialCheckLog checkLog);
    DryRunJobInfo DryRunJob(object schedulerConnectionConnection, DryRunJobSpecification dryRunJobSpecification);
}