﻿using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemCommands;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using log4net;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.Generic
{
    /// <summary>
    /// HyperQueue scheduler adapter
    /// </summary>
    internal class HyperQueueSchedulerAdapter : ISchedulerAdapter
    {
        #region Instances
        /// <summary>
        /// Convertor reference.
        /// </summary>
        protected ISchedulerDataConvertor _convertor;

        /// <summary>
        /// Commands
        /// </summary>
        protected ICommands _commands;

        /// <summary>
        /// Logger
        /// </summary>
        protected ILog _log;

        /// <summary>
        /// SSH tunnel
        /// </summary>
        protected static SshTunnelUtils _sshTunnelUtil;
        #endregion
        #region Constructors
        public HyperQueueSchedulerAdapter(ISchedulerDataConvertor convertor)
        {
            _log = LogManager.GetLogger(typeof(HyperQueueSchedulerAdapter));
            _convertor = convertor;
            _sshTunnelUtil = new SshTunnelUtils();
            _commands = new LinuxCommands();
        }
        #endregion
        #region ISchedulerAdapter Members
        public IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification,
            ClusterAuthenticationCredentials credentials)
        {
            var schedulerJobIdClusterAllocationNamePairs = new List<(string ScheduledJobId, string ClusterAllocationName)>();
            SshCommandWrapper command = null;
            string sshCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, "hq submit");
            _log.Info($"Submitting job \"{jobSpecification.Id}\", command \"{sshCommand}\"");
            string sshCommandBase64 = $"{_commands.InterpreterCommand} '{_commands.ExecuteCmdScriptPath} {Convert.ToBase64String(Encoding.UTF8.GetBytes(sshCommand))}'";
            
            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
                //^Job submitted successfully, job ID: (\d+)$
                // implement parser regex
                string jobIdPattern = @"^Job submitted successfully, job ID: (\d+)$";
                Match match = Regex.Match(command.Result, jobIdPattern);
                string jobId = match.Success ? match.Groups[1].Value : string.Empty;
                if (string.IsNullOrEmpty(jobId))
                {
                    throw new Exception($"Unable to parse job id from HQ server! Job id pattern: {jobIdPattern}, command result: {command.Result}");
                }
                
                schedulerJobIdClusterAllocationNamePairs.Add((jobId, jobSpecification.Tasks.First().ClusterNodeType.ClusterAllocationName));

                SubmittedTaskInfo taskInfo = GetActualHqJobInfo(connectorClient, jobSpecification.Cluster, jobId);

                return new List<SubmittedTaskInfo>() { taskInfo };
            }
            catch (FormatException e)
            {
                throw new Exception(@$"Exception thrown when submitting a job: ""{jobSpecification.Name}"" to the cluster: ""{jobSpecification.Cluster.Name}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script error message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommandBase64}"".\n", e);
            }
            return null;
        }

        private SubmittedTaskInfo GetActualHqJobInfo(object connectorClient, Cluster cluster, string jobId)
        {
            //create hq command and send to hq job info 1 --output-mode=json
            string sshCommand = $"ml HyperQueue && hq job info {jobId} --output-mode=json";
            SshCommandWrapper command = null;
            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
                return _convertor.ReadParametersFromResponse(cluster, command.Result).First();
            }
            catch (FormatException e)
            {
                throw new Exception(@$"Exception thrown when getting job info for job id: ""{jobId}"" from the cluster: ""{cluster.Name}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script error message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommand}"".\n", e);
            }
        }

        public IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster, IEnumerable<SubmittedTaskInfo> submitedTasksInfo)
        {
            var tasksInfo = new List<SubmittedTaskInfo>();
            foreach (var task in submitedTasksInfo)
            {
                var actualInfo = GetActualHqJobInfo(connectorClient, cluster, task.ScheduledJobId);
                if (string.IsNullOrEmpty(actualInfo.ScheduledJobId))
                {
                    task.State = actualInfo.State;
                    task.ErrorMessage = actualInfo.ErrorMessage;
                    tasksInfo.Add(task);
                    continue;
                }
                tasksInfo.Add(actualInfo);
            }
            return tasksInfo;
        }

        public void CancelJob(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message)
        {
            string sshCommand = $"ml HyperQueue && hq job cancel {string.Join(", ", submitedTasksInfo.Select(s => s.ScheduledJobId))}";
            SshCommandWrapper command = null;
            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
            }
            catch (FormatException e)
            {
                throw new Exception(@$"Exception thrown when cancelling job with id: ""{string.Join(", ", submitedTasksInfo.Select(s => s.ScheduledJobId))}"".
                                       Submission script result: ""{command.Result}"".\nSubmission script error message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommand}"".\n", e);
            }
        }

        public ClusterNodeUsage GetCurrentClusterNodeUsage(object connectorClient, ClusterNodeType nodeType)
        {
            throw new NotImplementedException("GetCurrentClusterNodeUsage is not supported for HyperQueue");
        }

        public IEnumerable<string> GetAllocatedNodes(object connectorClient, SubmittedTaskInfo taskInfo)
        {
            throw new NotImplementedException("GetAllocatedNodes is not supported for HyperQueue");
        }

        public IEnumerable<string> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath)
        {
            return _commands.GetParametersFromGenericUserScript(connectorClient, userScriptPath);
        }

        public void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo)
        {
            _commands.AllowDirectFileTransferAccessForUserToJob(connectorClient, publicKey, jobInfo);
        }

        public void RemoveDirectFileTransferAccessForUser(object connectorClient, IEnumerable<string> publicKeys)
        {
            _commands.RemoveDirectFileTransferAccessForUser(connectorClient, publicKeys);
        }

        public void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
            bool sharedAccountsPoolMode)
        {
            _commands.CreateJobDirectory(connectorClient, jobInfo, localBasePath, sharedAccountsPoolMode);
        }

        public void DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
        {
            _commands.DeleteJobDirectory(connectorClient, jobInfo, localBasePath);
        }

        public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash,
            string path)
        {
            _commands.CopyJobDataToTemp(connectorClient, jobInfo, localBasePath, hash, path);
        }

        public void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash, string localBasePath)
        {
            _commands.CopyJobDataFromTemp(connectorClient, jobInfo, localBasePath, hash);
        }

        public void CreateTunnel(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort)
        {
            _sshTunnelUtil.CreateTunnel(connectorClient, taskInfo.Id, nodeHost, nodePort);
        }

        public void RemoveTunnel(object connectorClient, SubmittedTaskInfo taskInfo)
        {
            _sshTunnelUtil.RemoveTunnel(connectorClient, taskInfo.Id);
        }

        public IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost)
        {
            return _sshTunnelUtil.GetTunnelsInformations(taskInfo.Id, nodeHost);
        }

        public void InitializeClusterScriptDirectory(object schedulerConnectionConnection, string clusterProjectRootDirectory,
            string localBasepath, bool isServiceAccount)
        {
            _commands.InitializeClusterScriptDirectory(schedulerConnectionConnection, clusterProjectRootDirectory, localBasepath, isServiceAccount);
        }
        #endregion
    }
}
