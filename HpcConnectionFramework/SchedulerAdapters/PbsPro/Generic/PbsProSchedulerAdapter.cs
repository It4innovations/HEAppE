using System;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.MiddlewareUtils;
using log4net;
using Renci.SshNet;
using System.Collections.Generic;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using System.Linq;
using System.Text.RegularExpressions;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using System.Text;
using HEAppE.HpcConnectionFramework.SystemCommands;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic
{
    internal class PbsProSchedulerAdapter : ISchedulerAdapter
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
        protected static SshTunnel _sshTunnelUtil;
        #endregion
        #region Constructors
        public PbsProSchedulerAdapter(ISchedulerDataConvertor convertor)
        {
            //TODO parse from DI
            _log = LogManager.GetLogger(typeof(PbsProSchedulerAdapter));
            _convertor = convertor;
            _sshTunnelUtil = new SshTunnel();
            _commands = new LinuxCommands();
        }
        #endregion

        #region ISchedulerAdapter Members
        public virtual IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials)
        {
            SshCommandWrapper command = null;
            string sshCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, "qsub");
            string sshCommandBase64 = $"bash -lc '~/.key_scripts/run_command.sh {Convert.ToBase64String(Encoding.UTF8.GetBytes(sshCommand))}'";
            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommandBase64);
                var jobIds = _convertor.GetJobIds(command.Result);

                return null;// GetActualJobInfo(scheduler, jobIds);
            }
            catch (FormatException e)
            {
                throw new Exception(@$"Exception thrown when submitting a job: ""{jobSpecification.Name}"" to the cluster: ""{jobSpecification.Cluster.Name}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script error message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommandBase64}"".\n", e);
            }
        }

        /// <summary>
        /// Get actual tasks
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="scheduledJobIds">Scheduler job ids</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, IEnumerable<string> scheduledJobIds)
        {
            //TODO rewrite
            const string JOB_ID_REGEX = @"^(Job\ Id:\ )([a-zA-Z0-9\.\[\]\-]+)";
            var scheduledJobIdsArray = scheduledJobIds.ToArray();
            SshCommandWrapper command = null;
            do
            {
                string commandString = $"bash -lc 'qstat -f -x {string.Join(" ", scheduledJobIdsArray)}'";
                try
                {
                    command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), commandString);
                }
                catch (SshCommandException ce)
                {
                    _log.Warn(ce.Message);
                    break;
                }
                catch (Exception e)
                {
                    string jobIdMath = "qstat: Unknown Job Id";
                    if (e.Message.Contains(jobIdMath))
                    {
                        Match match = Regex.Match(e.Message, @$"(?<={jobIdMath} )\b\w+[.\w]*\b", RegexOptions.Compiled);
                        if (match.Success)
                        {
                            string jobId = match.Value;
                            _log.Warn($"Unknown Job ID {jobId} in qstat output. Setting the job's status to Canceled and retry for remaining jobs.");
                            scheduledJobIdsArray = scheduledJobIdsArray?.Where(val => val != jobId)
                                                               .ToArray();
                            command = null;
                        }
                    }
                    else
                    {
                        _log.ErrorFormat(e.Message);
                    }
                }
            }
            while (command == null);


            if (command is not null)
            {
                string[] resultLines = command.Result.Split('\n');

                // Search for lines with jobIds
                Dictionary<string, int> jobLines = new Dictionary<string, int>();
                for (int i = 0; i < resultLines.Length; i++)
                {
                    Match match = Regex.Match(resultLines[i], JOB_ID_REGEX);
                    if (match.Success)
                    {
                        jobLines.Add(match.Groups[2].Value, i);
                    }
                }

                // Iterate through jobIds and extract task info
                List<SubmittedTaskInfo> taskInfos = new List<SubmittedTaskInfo>();
                for (int i = 0; i < scheduledJobIdsArray?.Length; i++)
                {
                    // Search for jobId in result
                    int jobInfoStartLine = -1;
                    try
                    {
                        jobInfoStartLine = jobLines[scheduledJobIdsArray[i]];
                    }
                    catch (KeyNotFoundException)
                    {
                        _log.ErrorFormat("Job ID {0} not found in qstat output.", scheduledJobIdsArray[i]);

                        taskInfos.Add(new SubmittedTaskInfo());
                        continue;
                    }

                    // Get number of lines in qstat output for this job
                    int jobInfoLineCount = 0;
                    do
                    {
                        jobInfoLineCount++;
                    } while (!Regex.IsMatch(resultLines[jobInfoStartLine + jobInfoLineCount], JOB_ID_REGEX) && jobInfoLineCount + jobInfoStartLine + 1 < resultLines.Length);

                    // Cut lines for given job info
                    string[] currentJobLines = new string[jobInfoLineCount];
                    Array.Copy(resultLines, jobInfoStartLine, currentJobLines, 0, jobInfoLineCount);

                    // Get current job info
                    //TODO
                    //taskInfos.Add(_convertor.ConvertTaskToTaskInfo(String.Join("\n", currentJobLines)));
                }
                return taskInfos;
            }
            else
            {
                return Enumerable.Empty<SubmittedTaskInfo>();
            }
        }

        /// <summary>
        /// Cancel job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="scheduledJobIds">Scheduled job ids</param>
        /// <param name="message">Message</param>
        public virtual void CancelJob(object connectorClient, IEnumerable<string> scheduledJobIds, string message)
        {
            StringBuilder cmdBuilder = new();
            scheduledJobIds.ToList().ForEach(f => cmdBuilder.Append($"bash -lc 'qdel {f}';"));
            string sshCommand = cmdBuilder.ToString();

            SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
        }

        public virtual ClusterNodeUsage GetCurrentClusterNodeUsage(object scheduler, ClusterNodeType nodeType)
        {
            //TODO rewrite
            ClusterNodeUsage usage = new ClusterNodeUsage
            {
                NodeType = nodeType
            };

            var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), $"bash -lc 'qstat -Q -f {nodeType.Queue}'");
            var resourcesParams = command.Result.Replace("\n\t", "")
                                                 .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                                                    .Skip(1)
                                                    .Select(item => item.Split("="))
                                                    .ToDictionary(s => s[0].Replace(" ", ""), s => s[1]);

            if (resourcesParams.TryGetValue(PbsProNodeUsageAttributes.RESOURCES_ASSIGNED_NODECT, out string nodesUsed))
            {
                usage.NodesUsed = StringUtils.ExtractInt(nodesUsed);
            }

            if (resourcesParams.TryGetValue(PbsProNodeUsageAttributes.QUEUE_TYPE_PRIORITY, out string priority))
            {
                usage.Priority = StringUtils.ExtractInt(priority);
            }

            if (resourcesParams.TryGetValue(PbsProNodeUsageAttributes.QUEUE_TYPE_TOTAL_JOBS, out string totalJobs))
            {
                usage.TotalJobs = StringUtils.ExtractInt(totalJobs);
            }

            return usage;
        }


        /// <summary>
        /// Get allocated nodes per job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>

        public IEnumerable<string> GetAllocatedNodes(object connectorClient, SubmittedJobInfo jobInfo)
        {
            //TODO rewrite
#warning this should use database instead of direct read from file
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"cat {jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath}/{jobInfo.Specification.Id}/AllocationNodeInfo");
            _log.InfoFormat("Allocated nodes: {0}", sshCommand.Result);
            return null;// _convertor.ConvertNodesUrlsToList(sshCommand.Result);
        }

        /// <summary>
        /// Get generic command templates parameters from script
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="userScriptPath">Generic script path</param>
        /// <returns></returns>
        public virtual IEnumerable<string> GetParametersFromGenericUserScript(object scheduler, string userScriptPath)
        {
            var genericCommandParameters = new List<string>();
            string shellCommand = $"cat {userScriptPath}";
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);

            foreach (Match match in Regex.Matches(sshCommand.Result, @$"{HPCConnectionFrameworkConfiguration.GenericCommandKeyParameter}([\s\t]+[A-z_\-]+)\n", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                if (match.Success && match.Groups.Count == 2)
                {
                    genericCommandParameters.Add(match.Groups[1].Value.TrimStart());
                }
            }
            return genericCommandParameters;
        }

        /// <summary>
        /// Allow direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job info</param>
        public void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo)
        {
            _commands.AllowDirectFileTransferAccessForUserToJob(connectorClient, publicKey, jobInfo);
        }

        /// <summary>
        /// Remove direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Conenctor</param>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job info</param>
        public void RemoveDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo)
        {
            _commands.RemoveDirectFileTransferAccessForUserToJob(connectorClient, publicKey, jobInfo);
        }

        /// <summary>
        /// Create job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        public void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo)
        {
            _commands.CreateJobDirectory(connectorClient, jobInfo);
        }

        /// <summary>
        /// Delete job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        public void DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo)
        {
            _commands.DeleteJobDirectory(connectorClient, jobInfo);
        }

        /// <summary>
        /// Copy job data from temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash, string path)
        {
            _commands.CopyJobDataToTemp(connectorClient, jobInfo, hash, path);
        }

        /// <summary>
        /// Copy job data to temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        /// <param name="path">Path</param>
        public void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash)
        {
            _commands.CopyJobDataFromTemp(connectorClient, jobInfo, hash);
        }
        #region SSH tunnel methods
        /// <summary>
        /// Create SSH tunnel
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="localHost">Local host</param>
        /// <param name="localPort">Local port</param>
        /// <param name="loginHost">Login host</param>
        /// <param name="nodeHost">Node host</param>
        /// <param name="nodePort">Node port</param>
        /// <param name="credentials">Credentials</param>
        public void CreateSshTunnel(long jobId, string localHost, int localPort, string loginHost, string nodeHost, int nodePort, ClusterAuthenticationCredentials credentials)
        {
            _sshTunnelUtil.CreateSshTunnel(jobId, localHost, localPort, loginHost, nodeHost, nodePort, credentials);
        }

        /// <summary>
        /// Remove SSH tunnel
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="nodeHost">Node host</param>
        public void RemoveSshTunnel(long jobId, string nodeHost)
        {
            _sshTunnelUtil.RemoveSshTunnel(jobId, nodeHost);
        }

        /// <summary>
        /// Check if SSH tunnel exist
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="nodeHost">Node host</param>
        /// <returns></returns>
        public bool SshTunnelExist(long jobId, string nodeHost)
        {
            return _sshTunnelUtil.SshTunnelExist(jobId, nodeHost);
        }
        #endregion
        #endregion
    }
}