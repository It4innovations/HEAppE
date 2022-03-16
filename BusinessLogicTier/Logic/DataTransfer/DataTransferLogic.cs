using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.DataTransfer.Exceptions;
using HEAppE.BusinessLogicTier.Logic.JobManagement;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.DataTransfer;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.Exceptions;
using log4net;
using RestSharp;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.BusinessLogicTier.Logic.DataTransfer
{
    /// <summary>
    /// Data transfer logic
    /// </summary>
    public class DataTransferLogic : IDataTransferLogic
    {
        #region Instances
        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILog _logger;

        /// <summary>
        /// Unit of work
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Management logic
        /// </summary>
        private readonly IJobManagementLogic _managementLogic;

        /// <summary>
        /// HashSet tasks with opened tunnel
        /// </summary>
        private static readonly HashSet<long> _taskWithExistingTunnel = new();

        /// <summary>
        /// Lock tunnel object
        /// </summary>
        private readonly object _lockTunnelObj = new();
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="unitOfWork">Unit of work</param>
        public DataTransferLogic(IUnitOfWork unitOfWork)
        {
            _logger = LogManager.GetLogger(typeof(DataTransferLogic));
            _unitOfWork = unitOfWork;
            _managementLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork);
        }
        #endregion
        #region IDataTransferLogic Members
        /// <summary>
        /// Create tunnel to cluster node
        /// </summary>
        /// <param name="nodeIPAddress">Node IP address</param>
        /// <param name="nodePort">Node port</param>
        /// <param name="submittedTaskInfoId">Submitted task information id</param>
        /// <param name="loggedUser">Logged user</param>
        /// <returns></returns>
        /// <exception cref="UnableToCreateConnectionException"></exception>
        public DataTransferMethod GetDataTransferMethod(string nodeIPAddress, int nodePort, long submittedTaskInfoId, AdaptorUser loggedUser)
        {
            try
            {
                var taskInfo = _managementLogic.GetSubmittedTaskInfoById(submittedTaskInfoId, loggedUser);
                _logger.Info($"Getting data transfer method for submitted task id: \"{submittedTaskInfoId}\" with user: \"{loggedUser.GetLogIdentification()}\"");

                if (taskInfo.State == TaskState.Running)
                {
                    lock (_lockTunnelObj)
                    {
                        var cluster = taskInfo.Specification.ClusterNodeType.Cluster;
                        var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster);

                        var getTunnelsInfos = scheduler.GetTunnelsInfos(taskInfo, nodeIPAddress);
                        if (getTunnelsInfos.Any(f => f.RemotePort == nodePort))
                        {
                            throw new UnableToCreateConnectionException($"Task id: \"{submittedTaskInfoId}\" with remote node IP address: \"{nodeIPAddress}\" already has ssh tunnel for port: \"{nodePort}\".");
                        }

                        scheduler.CreateTunnel(taskInfo, nodeIPAddress, nodePort);
                        _taskWithExistingTunnel.Add(submittedTaskInfoId);
                        var tunnelInfo = scheduler.GetTunnelsInfos(taskInfo, nodeIPAddress).Where(w => w.RemotePort == nodePort)
                                                                                                     .FirstOrDefault();

                        if (tunnelInfo is null)
                        {
                            throw new UnableToCreateConnectionException($"Task id: \"{submittedTaskInfoId}\" with remote node IP address: \"{nodeIPAddress}\" already has ssh tunnel for port: \"{nodePort}\".");
                        }

                        return new DataTransferMethod
                        {
                            SubmittedTaskId = taskInfo.Id,
                            Port = tunnelInfo.LocalPort,
                            NodeIPAddress = tunnelInfo.NodeHost,
                            NodePort = tunnelInfo.RemotePort
                        };
                    }
                }
                else
                {
                    throw new UnableToCreateConnectionException($"Task id: \"{taskInfo.Id}\" is not a currently running task.");
                }
            }
            catch (UnableToCreateTunnelException tunExc)
            {
                throw new UnableToCreateConnectionException(tunExc.Message);
            }
        }

        /// <summary>
        /// Close tunnel to cluster node
        /// </summary>
        /// <param name="transferMethod"></param>
        /// <param name="loggedUser">Logged user</param>
        public void EndDataTransfer(DataTransferMethod transferMethod, AdaptorUser loggedUser)
        {
            try
            {
                var taskInfo = _managementLogic.GetSubmittedTaskInfoById(transferMethod.SubmittedTaskId, loggedUser);
                _logger.Info($"Removing data transfer method for submitted task id: \"{taskInfo.Id}\" with user: \"{loggedUser.GetLogIdentification()}\"");

                var cluster = taskInfo.Specification.ClusterNodeType.Cluster;
                lock (_lockTunnelObj)
                {
                    SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster).RemoveTunnel(taskInfo);
                    _taskWithExistingTunnel.Remove(taskInfo.Id);
                }
            }
            catch (UnableToCreateTunnelException tunExc)
            {
                throw new UnableToCreateConnectionException(tunExc.Message);
            }
        }

        /// <summary>
        /// Get task ids with opened tunnel
        /// </summary>
        /// <returns></returns>
        public IEnumerable<long> GetTaskIdsWithOpenTunnels()
        {
            _logger.Info("Listing all tasks with open tunnels:");
            return _taskWithExistingTunnel;
        }

        /// <summary>
        /// Close all tunnels for finished tasks
        /// </summary>
        /// <param name="taskInfo">Task Info</param>
        public void CloseAllTunnelsForTask(SubmittedTaskInfo taskInfo)
        {
            try
            {
                _logger.Info($"Closing all tunnels for task id: \"{taskInfo.Id}\"");

                var scheduler = SchedulerFactory.GetInstance(taskInfo.Specification.JobSpecification.Cluster.SchedulerType).CreateScheduler(taskInfo.Specification.JobSpecification.Cluster);
                lock (_lockTunnelObj)
                {
                    scheduler.RemoveTunnel(taskInfo);
                    _taskWithExistingTunnel.Remove(taskInfo.Id);
                }
            }
            catch (UnableToCreateTunnelException tunExc)
            {
                throw new UnableToCreateConnectionException(tunExc.Message);
            }
        }

        public async Task<string> HttpGetToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeader> headers, long submittedTaskInfoId, string nodeIPAddress, int nodePort, AdaptorUser loggedUser)
        {
            try
            {
                var taskInfo = _managementLogic.GetSubmittedTaskInfoById(submittedTaskInfoId, loggedUser);
                _logger.Info($"HTTP GET from task: \"{submittedTaskInfoId}\" with remote node IP address: \"{nodeIPAddress}\" HTTP request: \"{httpRequest}\" HTTP headers: \"{string.Join(",", headers)}\"");

                var cluster = taskInfo.Specification.ClusterNodeType.Cluster;
                var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster);
                var getTunnelsInfos = scheduler.GetTunnelsInfos(taskInfo, nodeIPAddress);

                if (!getTunnelsInfos.Any(f => f.RemotePort == nodePort))
                {
                    throw new UnableToCreateConnectionException($"Task \"{submittedTaskInfoId}\" with node IP address: \"{nodeIPAddress}\" does not have an active connection");
                }

                var allocatedPort = getTunnelsInfos.First(f => f.RemotePort == nodePort).LocalPort.Value;
                var options = new RestClientOptions($"http://localhost:{allocatedPort}")
                {
                    Encoding = Encoding.UTF8,
                    CachePolicy = new CacheControlHeaderValue()
                    {
                        NoCache = true,
                        NoStore = true
                    },
                    Timeout = BusinessLogicConfiguration.HTTPRequestConnectionTimeout
                };
                var basicRestClient = new RestClient(options);

                var request = new RestRequest(httpRequest, Method.Get);
                headers.ToList().ForEach(f => request.AddHeader(f.Name, f.Value));

                var response = await basicRestClient.ExecuteAsync(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new UnableToCreateConnectionException($"Response code for HttpGet is not 200. Check your application.");
                }

                return response.Content;
            }
            catch (UnableToCreateTunnelException tunExc)
            {
                throw new UnableToCreateConnectionException(tunExc.Message);
            }
        }

        public async Task<string> HttpPostToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeader> headers, string httpPayload, long submittedTaskInfoId, string nodeIPAddress, int nodePort, AdaptorUser loggedUser)
        {
            try
            {
                var taskInfo = _managementLogic.GetSubmittedTaskInfoById(submittedTaskInfoId, loggedUser);
                _logger.Info($"HTTP POST from task: \"{submittedTaskInfoId}\" with remote node IP address: \"{nodeIPAddress}\" HTTP request: \"{httpRequest}\" HTTP headers: \"{string.Join(",", headers)}\" HTTP Payload: \"{httpPayload}\"");

                var cluster = taskInfo.Specification.ClusterNodeType.Cluster;
                var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster);
                var getTunnelsInfos = scheduler.GetTunnelsInfos(taskInfo, nodeIPAddress);

                if (!getTunnelsInfos.Any(f => f.RemotePort == nodePort))
                {
                    throw new UnableToCreateConnectionException($"Task \"{submittedTaskInfoId}\" with node IP address: \"{nodeIPAddress}\" does not have an active connection");
                }

                var allocatedPort = getTunnelsInfos.First(f => f.RemotePort == nodePort).LocalPort.Value;
                var options = new RestClientOptions($"http://localhost:{allocatedPort}")
                {
                    Encoding = Encoding.UTF8,
                    CachePolicy = new CacheControlHeaderValue()
                    {
                        NoCache = true,
                        NoStore = true
                    },
                    Timeout = BusinessLogicConfiguration.HTTPRequestConnectionTimeout
                };
                var basicRestClient = new RestClient(options);

                var request = new RestRequest(httpRequest, Method.Get);
                headers.ToList().ForEach(f => request.AddHeader(f.Name, f.Value));

                //Body part
                byte[] payload = Encoding.UTF8.GetBytes(httpPayload);
                request.AddHeader("content-type", "application/x-www-form-urlencoded")
                       .AddHeader("contentLength", payload.Length)
                       .AddBody(payload);

                var response = await basicRestClient.ExecuteAsync(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new UnableToCreateConnectionException($"Response code for HttpPost is not 200. Check your application.");
                }

                return response.Content;
            }
            catch (UnableToCreateTunnelException tunExc)
            {
                throw new UnableToCreateConnectionException(tunExc.Message);
            }
        }
        #endregion
    }
}