﻿using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces
{
    /// <summary>
    /// IResx scheduler
    /// </summary>
    public interface IRexScheduler
    {
        IEnumerable<SubmittedTaskInfo> SubmitJob(JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials);

        IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(IEnumerable<SubmittedTaskInfo> submitedTasksInfo, ClusterAuthenticationCredentials credentials);

        void CancelJob(IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message, ClusterAuthenticationCredentials credentials);

        ClusterNodeUsage GetCurrentClusterNodeUsage(ClusterNodeType nodeType);

        IEnumerable<string> GetAllocatedNodes(SubmittedTaskInfo taskInfo);

        IEnumerable<string> GetParametersFromGenericUserScript(Cluster cluster, string userScriptPath);

        void AllowDirectFileTransferAccessForUserToJob(string publicKey, SubmittedJobInfo jobInfo);

        void RemoveDirectFileTransferAccessForUser(IEnumerable<string> publicKeys, ClusterAuthenticationCredentials credentials);

        void CreateJobDirectory(SubmittedJobInfo jobInfo);

        void DeleteJobDirectory(SubmittedJobInfo jobInfo);

        void CopyJobDataToTemp(SubmittedJobInfo jobInfo, string hash, string path);

        void CopyJobDataFromTemp(SubmittedJobInfo jobInfo, string hash);

        void CreateTunnel(SubmittedTaskInfo taskInfo, string nodeHost, int nodePort);

        void RemoveTunnel(SubmittedTaskInfo taskInfo);

        IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost);
    }
}