using System;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.MiddlewareUtils;
using log4net;
using Renci.SshNet;
using System.Collections.Generic;
using Renci.SshNet.Common;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using System.Linq;

namespace HEAppE.HpcConnectionFramework.LinuxPbs.v10
{
    public class LinuxPbsV10SchedulerAdapter : ISchedulerAdapter
    {
        #region Constructors
        public LinuxPbsV10SchedulerAdapter(ISchedulerDataConvertor convertor)
        {
            _convertor = convertor;
            _log = LogManager.GetLogger(typeof(LinuxPbsV10SchedulerAdapter));
        }
        #endregion

        #region ISchedulerAdapter Members
        public virtual SubmittedJobInfo SubmitJob(object scheduler, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials)
        {
            //string job = "bash -lc '" + (string) _convertor.ConvertJobSpecificationToJob(jobSpecification, "qsub") + "'";
            string job = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, "qsub");
            var command = RunSshCommand(new SshClientAdapter((SshClient)scheduler), job);
            _log.Info(command.Result);
            try
            {
                string jobId = LinuxPbsConversionUtils.GetJobIdFromJobCode(command.Result);
                return GetActualJobInfo(scheduler, jobId);
            }
            catch (FormatException e)
            {
                throw new Exception(
                    "Exception thrown when submitting a job to the cluster. Submission script result: " + command.Result +
                    "\nCommand line for job submission:\n" + job, e);
            }
        }

        public virtual void CancelJob(object scheduler, string scheduledJobId, string message)
        {
            RunSshCommand(new SshClientAdapter((SshClient)scheduler), String.Format("bash -lc 'qdel {0}'", scheduledJobId));
        }

        public virtual SubmittedJobInfo GetActualJobInfo(object scheduler, string scheduledJobId)
        {
            var command = RunSshCommand(new SshClientAdapter((SshClient)(scheduler)), String.Format("bash -lc 'qstat -f {0}'", scheduledJobId));
            return _convertor.ConvertJobToJobInfo(command.Result);
        }

        public virtual SubmittedJobInfo GetActualJobInfo(object scheduler, string[] scheduledJobIds)
        {
            throw new NotImplementedException();
        }

        public virtual SubmittedTaskInfo[] GetActualTasksInfo(object scheduler, string[] scheduledJobIds)
        {
            throw new NotImplementedException();
        }

        public virtual ClusterNodeUsage GetCurrentClusterNodeUsage(object scheduler, ClusterNodeType nodeType)
        {
            ClusterNodeUsage usage = new ClusterNodeUsage
            {
                NodeType = nodeType
            };

            var command = RunSshCommand(new SshClientAdapter((SshClient)scheduler), $"bash -lc 'qstat -Q -f {nodeType.Queue}'");
            var resourcesParams = command.Result.Replace("\n\t", "")
                                                 .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                                                    .Skip(1)
                                                    .Select(item => item.Split("="))
                                                    .ToDictionary(s => s[0].Replace(" ", ""), s => s[1]);

            if (resourcesParams.TryGetValue(LinuxPbsNodeUsageAttributes.RESOURCES_ASSIGNED_NODECT, out string nodesUsed))
            {
                usage.NodesUsed = StringUtils.ExtractInt(nodesUsed);
            }

            if (resourcesParams.TryGetValue(LinuxPbsNodeUsageAttributes.QUEUE_TYPE_PRIORITY, out string priority))
            {
                usage.Priority = StringUtils.ExtractInt(priority);
            }

            if (resourcesParams.TryGetValue(LinuxPbsNodeUsageAttributes.QUEUE_TYPE_TOTAL_JOBS, out string totalJobs))
            {
                usage.TotalJobs = StringUtils.ExtractInt(totalJobs);
            }

            return usage;
        }


        public virtual List<string> GetAllocatedNodes(object scheduler, SubmittedJobInfo jobInfo)
        {
#warning this should use database instead of direct read from file
            string shellCommand = String.Format("cat {0}/{1}/nodefile", jobInfo.Specification.Cluster.LocalBasepath, jobInfo.Specification.Id);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Allocated nodes: {0}", sshCommand.Result);
            return LinuxPbsConversionUtils.ConvertNodesUrlsToList(sshCommand.Result);
        }

        public virtual void AllowDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo)
        {
            publicKey = StringUtils.RemoveWhitespace(publicKey);
            string shellCommand = String.Format("~/.key_scripts/add_key.sh {0} {1}", publicKey, jobInfo.Specification.Id);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.Info(String.Format("Allow file transfer result: {0}", sshCommand.Result));
        }

        public virtual void RemoveDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo)
        {
            publicKey = StringUtils.RemoveWhitespace(publicKey);
            string shellCommand = String.Format("~/.key_scripts/remove_key.sh {0}", publicKey);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.Info(String.Format("Remove permission for direct file transfer result: {0}", sshCommand.Result));
        }

        public virtual void CreateJobDirectory(object scheduler, SubmittedJobInfo jobInfo)
        {
            string shellCommand = String.Format("~/.key_scripts/create_job_directory.sh {0}/{1}", jobInfo.Specification.Cluster.LocalBasepath, jobInfo.Specification.Id);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Create job directory result: {0}", sshCommand.Result);
        }

        public virtual void DeleteJobDirectory(object scheduler, SubmittedJobInfo jobInfo)
        {
            string shellCommand = String.Format("rm -Rf {0}/{1}", jobInfo.Specification.Cluster.LocalBasepath, jobInfo.Specification.Id);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Job directory {0} was deleted", jobInfo.Specification.Id);
        }

        public virtual SubmittedJobInfo[] GetActualJobsInfo(object scheduler, int[] scheduledJobIds)
        {
            throw new NotImplementedException();
        }

        public virtual void CopyJobDataToTemp(object scheduler, SubmittedJobInfo jobInfo, string hash, string path)
        {
            //if path is null or empty then all files and directories from ClusterLocalBasepath will be copied to hash directory
            string inputDirectory = String.Format("{0}/{1}/{2}", jobInfo.Specification.Cluster.LocalBasepath, jobInfo.Specification.Id, path);
            string outputDirectory = String.Format("{0}Temp/{1}", jobInfo.Specification.Cluster.LocalBasepath, hash);
            inputDirectory += string.IsNullOrEmpty(path) ? "." : string.Empty;//copy just content
            string shellCommand = String.Format("~/.key_scripts/copy_data_to_temp.sh {0} {1}", inputDirectory, outputDirectory);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Job data {0}/{1} were copied to temp directory {2}, result: {3}", jobInfo.Specification.Id, path, hash, sshCommand.Result);
        }

        public virtual void CopyJobDataFromTemp(object scheduler, SubmittedJobInfo jobInfo, string hash)
        {
            string inputDirectory = String.Format("{0}Temp/{1}/.", jobInfo.Specification.Cluster.LocalBasepath, hash);
            string outputDirectory = String.Format("{0}/{1}", jobInfo.Specification.Cluster.LocalBasepath, jobInfo.Specification.Id);
            string shellCommand = String.Format("~/.key_scripts/copy_data_from_temp.sh {0} {1}", inputDirectory, outputDirectory);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Temp data {0} were copied to job directory {1}, result: {2}", hash, jobInfo.Specification.Id, sshCommand.Result);
        }

        public virtual void CreateSshTunnel(long jobId, string localHost, int localPort, string loginHost, string nodeHost, int nodePort, ClusterAuthenticationCredentials credentials)
        {
            if (jobHostTunnels.ContainsKey(jobId) && jobHostTunnels[jobId].ContainsKey(nodeHost))
            {
                _log.ErrorFormat("JobId {0} with IPaddress {1} already has a socket connection.", jobId, nodeHost);
            }
            else
            {
                SshClient sshTunnel = new SshClient(loginHost, credentials.Username, new PrivateKeyFile(credentials.PrivateKeyFile, credentials.PrivateKeyPassword));
                sshTunnel.Connect();

                var forwPort = new ForwardedPortLocal(localHost, (uint)localPort, nodeHost, (uint)nodePort);
                sshTunnel.AddForwardedPort(forwPort);

                forwPort.Exception += delegate (object sender, ExceptionEventArgs e)
                {
                    _log.Error(e.Exception.ToString());
                };
                forwPort.Start();

                if (jobHostTunnels.ContainsKey(jobId))
                    jobHostTunnels[jobId].Add(nodeHost, sshTunnel);
                else jobHostTunnels.Add(jobId, new Dictionary<string, SshClient> { { nodeHost, sshTunnel } });

                _log.InfoFormat("Ssh tunel for jobId {0} and node IPaddress {1}:{2} created. Local endpoint {3}:{4}", jobId, nodeHost, nodePort, localHost, localPort);
            }
        }

        public virtual void RemoveSshTunnel(long jobId, string nodeHost)
        {
            if (jobHostTunnels.ContainsKey(jobId))
            {
                if (jobHostTunnels[jobId].ContainsKey(nodeHost))
                {
                    if (jobHostTunnels[jobId][nodeHost] != null)
                    {
                        foreach (ForwardedPort port in jobHostTunnels[jobId][nodeHost].ForwardedPorts)
                            port.Stop();

                        jobHostTunnels[jobId][nodeHost].Disconnect();
                        jobHostTunnels[jobId].Remove(nodeHost);
                    }
                }
                if (jobHostTunnels[jobId].Count == 0)
                    jobHostTunnels.Remove(jobId);

                _log.InfoFormat("Ssh tunel for jobId {0} and node IPaddress {1} removed.", jobId, nodeHost);
            }
            else _log.InfoFormat("Ssh tunel for jobId {0} and node IPaddress {1} - nothing to remove.", jobId, nodeHost);
        }

        public virtual bool SshTunnelExist(long jobId, string nodeHost)
        {
            if (jobHostTunnels.ContainsKey(jobId) && jobHostTunnels[jobId].ContainsKey(nodeHost))
                return true;
            else return false;
        }

        #endregion

        #region Protected methods
        protected SshCommandWrapper RunSshCommand(SshClientAdapter client, string command)
        {
            SshCommandWrapper sshcmd = client.RunCommand(command);

            if (sshcmd.ExitStatus != 0)
            {
                throw new SshCommandException($"SSH command error: {sshcmd.Error} Error code: {sshcmd.ExitStatus} SSH command: {sshcmd.CommandText}");
            }
            if (sshcmd.Error.Length > 0)
            {
                _log.WarnFormat("SSH command finished with error: {0}", sshcmd.Error);
            }
            return sshcmd;
        }

        protected void CopyDirectory(object scheduler, string inputDirectory, string outputDirectory)
        {
            string shellCommand = String.Format("~/.key_scripts/move_data.sh {0} {1}", inputDirectory, outputDirectory);
            RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
        }

        protected void SetPermissions(object scheduler, string directory)
        {
            string shellCommand = String.Format("~/.key_scripts/set_permissions.sh {0}", directory);
            RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
        }
        #endregion

        #region Instance Fields
        /// <summary>
        ///   Convertor reference.
        /// </summary>
        protected ISchedulerDataConvertor _convertor;

        /// <summary>
        ///   Log4Net logger
        /// </summary>
        protected ILog _log;

        private static Dictionary<long, Dictionary<string, SshClient>> jobHostTunnels = new Dictionary<long, Dictionary<string, SshClient>>();

        #endregion



    }
}