using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.JobManagement;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.DataTransfer;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.Exceptions.External;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using log4net;
using RestSharp;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier.Logic.DataTransfer;

/// <summary>
///     Data transfer logic
/// </summary>
public class DataTransferLogic : IDataTransferLogic
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="unitOfWork">Unit of work</param>
    public DataTransferLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        _logger = LogManager.GetLogger(typeof(DataTransferLogic));
        _unitOfWork = unitOfWork;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _managementLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
    }

    #endregion

    #region Instances

    /// <summary>
    ///     Logger
    /// </summary>
    private readonly ILog _logger;

    /// <summary>
    ///     Unit of work
    /// </summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    ///     Management logic
    /// </summary>
    private readonly IJobManagementLogic _managementLogic;

    /// <summary>
    ///     HashSet tasks with opened tunnel
    /// </summary>
    private static readonly HashSet<long> _taskWithExistingTunnel = new();

    /// <summary>
    ///     Lock tunnel object
    /// </summary>
    private readonly object _lockTunnelObj = new();
    
    ISshCertificateAuthorityService _sshCertificateAuthorityService;

    private IHttpContextKeys _httpContextKeys;

    #endregion

    #region IDataTransferLogic Members

    /// <summary>
    ///     Create tunnel to cluster node
    /// </summary>
    /// <param name="nodeIPAddress">Node IP address</param>
    /// <param name="nodePort">Node port</param>
    /// <param name="submittedTaskInfoId">Submitted task information id</param>
    /// <param name="loggedUser">Logged user</param>
    /// <returns></returns>
    /// <exception cref="UnableToCreateConnectionException"></exception>
    public DataTransferMethod GetDataTransferMethod(string nodeIPAddress, int nodePort, long submittedTaskInfoId,
        AdaptorUser loggedUser)
    {
        var taskInfo = _managementLogic.GetSubmittedTaskInfoById(submittedTaskInfoId, loggedUser);
        _logger.Info(
            $"Getting data transfer method for submitted task id: \"{submittedTaskInfoId}\" with user: \"{loggedUser.GetLogIdentification()}\"");

        if (taskInfo.State == TaskState.Running)
            lock (_lockTunnelObj)
            {
                var cluster = taskInfo.Specification.ClusterNodeType.Cluster;
                var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType)
                    .CreateScheduler(cluster, taskInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id);

                var getTunnelsInfos = scheduler.GetTunnelsInfos(taskInfo, nodeIPAddress);
                if (getTunnelsInfos.Any(f => f.RemotePort == nodePort))
                    throw new UnableToCreateConnectionException("PortAlreadyInUse", submittedTaskInfoId, nodeIPAddress,
                        nodePort);

                scheduler.CreateTunnel(taskInfo, nodeIPAddress, nodePort, _httpContextKeys.Context.SshCaToken);
                _taskWithExistingTunnel.Add(submittedTaskInfoId);
                var tunnelInfo = scheduler.GetTunnelsInfos(taskInfo, nodeIPAddress).Where(w => w.RemotePort == nodePort)
                    .FirstOrDefault();

                return tunnelInfo is null
                    ? throw new UnableToCreateConnectionException("PortAlreadyInUse", submittedTaskInfoId,
                        nodeIPAddress, nodePort)
                    : new DataTransferMethod
                    {
                        SubmittedTaskId = taskInfo.Id,
                        Port = tunnelInfo.LocalPort,
                        NodeIPAddress = tunnelInfo.NodeHost,
                        NodePort = tunnelInfo.RemotePort
                    };
            }

        throw new UnableToCreateConnectionException("NotRunningTask", taskInfo.Id);
    }

    /// <summary>
    ///     Close tunnel to cluster node
    /// </summary>
    /// <param name="transferMethod"></param>
    /// <param name="loggedUser">Logged user</param>
    public void EndDataTransfer(DataTransferMethod transferMethod, AdaptorUser loggedUser)
    {
        var taskInfo = _managementLogic.GetSubmittedTaskInfoById(transferMethod.SubmittedTaskId, loggedUser);
        _logger.Info(
            $"Removing data transfer method for submitted task id: \"{taskInfo.Id}\" with user: \"{loggedUser.GetLogIdentification()}\"");

        var cluster = taskInfo.Specification.ClusterNodeType.Cluster;
        lock (_lockTunnelObj)
        {
            SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, taskInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id)
                .RemoveTunnel(taskInfo, _httpContextKeys.Context.SshCaToken);
            _taskWithExistingTunnel.Remove(taskInfo.Id);
        }
    }

    /// <summary>
    ///     Get task ids with opened tunnel
    /// </summary>
    /// <returns></returns>
    public IEnumerable<long> GetTaskIdsWithOpenTunnels()
    {
        return _taskWithExistingTunnel;
    }

    /// <summary>
    ///     Close all tunnels for finished tasks
    /// </summary>
    /// <param name="taskInfo">Task Info</param>
    public void CloseAllTunnelsForTask(SubmittedTaskInfo taskInfo)
    {
        _logger.Info($"Closing all tunnels for task id: \"{taskInfo.Id}\"");

        var scheduler = SchedulerFactory.GetInstance(taskInfo.Specification.JobSpecification.Cluster.SchedulerType)
            .CreateScheduler(taskInfo.Specification.JobSpecification.Cluster, taskInfo.Project, _sshCertificateAuthorityService, adaptorUserId: taskInfo.Specification.JobSpecification.Submitter.Id);
        lock (_lockTunnelObj)
        {
            scheduler.RemoveTunnel(taskInfo, _httpContextKeys.Context.SshCaToken);
            _taskWithExistingTunnel.Remove(taskInfo.Id);
        }
    }

    public async Task<string> HttpGetToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeader> headers,
        long submittedTaskInfoId, string nodeIPAddress, int nodePort, AdaptorUser loggedUser)
    {
        var taskInfo = _managementLogic.GetSubmittedTaskInfoById(submittedTaskInfoId, loggedUser);
        _logger.Info(
            $"HTTP GET from task: \"{submittedTaskInfoId}\" with remote node IP address: \"{nodeIPAddress}\" HTTP request: \"{httpRequest}\" HTTP headers: \"{string.Join(",", headers.Select(h=>$"({h.Name}, {h.Value})"))}\"");

        var cluster = taskInfo.Specification.ClusterNodeType.Cluster;
        var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, taskInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id);
        var getTunnelsInfos = scheduler.GetTunnelsInfos(taskInfo, nodeIPAddress);

        if (!getTunnelsInfos.Any(f => f.RemotePort == nodePort))
            throw new UnableToCreateConnectionException("NoActiveConnection", submittedTaskInfoId, nodeIPAddress);

        var allocatedPort = getTunnelsInfos.LastOrDefault(f => f.RemotePort == nodePort).LocalPort.Value;
        _logger.Info(
            $"Allocated port for task: \"{submittedTaskInfoId}\" with remote node IP address: \"{nodeIPAddress}\" is: \"{allocatedPort}\"");
        
        var options = new RestClientOptions($"http://localhost:{allocatedPort}")
        {
            Encoding = Encoding.UTF8,
            CachePolicy = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true
            },
            Timeout = TimeSpan.FromMilliseconds(BusinessLogicConfiguration.HTTPRequestConnectionTimeoutInSeconds * 1000)
        };
        var basicRestClient = new RestClient(options);

        var request = new RestRequest(httpRequest);
        headers.ToList().ForEach(f => request.AddHeader(f.Name, f.Value));

        var response = await basicRestClient.ExecuteAsync(request);
       
        if((int)response.StatusCode == 0 && response.ErrorMessage.Contains("Connection refused"))
        {
            _logger.Error($"Connection refused for task ID: {submittedTaskInfoId} on node IP: {nodeIPAddress} and port: {nodePort}");
            _logger.Info($"Attempting to recreate tunnel for task ID: {submittedTaskInfoId} on node IP: {nodeIPAddress} and port: {nodePort}");
            //try to open the tunnel again
            lock (_lockTunnelObj)
            {
                _logger.Info($"Recreating tunnel for task ID: {submittedTaskInfoId} on node IP: {nodeIPAddress} and port: {nodePort}");
                scheduler.RemoveTunnel(taskInfo, _httpContextKeys.Context.SshCaToken);
                scheduler.CreateTunnel(taskInfo, nodeIPAddress, nodePort, _httpContextKeys.Context.SshCaToken);
                _taskWithExistingTunnel.Add(submittedTaskInfoId);
                _logger.Info($"Tunnel recreated for task ID: {submittedTaskInfoId} on node IP: {nodeIPAddress} and port: {nodePort}");
            }
            var tunnel = scheduler.GetTunnelsInfos(taskInfo, nodeIPAddress)
                .LastOrDefault(f => f.RemotePort == nodePort);

            if (tunnel == null || tunnel.LocalPort == null)
            {
                throw new InvalidOperationException($"No tunnel found for RemotePort={nodePort} on node {nodeIPAddress}.");
            }
            allocatedPort = tunnel.LocalPort.Value;
            _logger.Info($"New allocated port after tunnel recreation: {allocatedPort}");
            options = new RestClientOptions($"http://localhost:{allocatedPort}")
            {
                Encoding = Encoding.UTF8,
                CachePolicy = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true
                },
                Timeout = TimeSpan.FromMilliseconds(BusinessLogicConfiguration.HTTPRequestConnectionTimeoutInSeconds * 1000)
            };
            basicRestClient = new RestClient(options);
            request = new RestRequest(httpRequest);
            //retry the request
            response = await basicRestClient.ExecuteAsync(request);
        }
        
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine($"HTTP GET failed for task ID: {submittedTaskInfoId}");
            logBuilder.AppendLine($"StatusCode: {(int)response.StatusCode} {response.StatusCode}");
            logBuilder.AppendLine($"Content: {response.Content}");
            logBuilder.AppendLine($"ErrorMessage: {response.ErrorMessage}");
            logBuilder.AppendLine($"ResponseUri: {response.ResponseUri}");
            logBuilder.AppendLine($"RequestUri (resource): {request.Resource}");
            logBuilder.AppendLine($"AllocatedPort: {allocatedPort}");
            logBuilder.AppendLine($"NodeIPAddress: {nodeIPAddress}");
            logBuilder.AppendLine($"NodePort: {nodePort}");
            _logger.Info(logBuilder.ToString());

            throw new UnableToCreateConnectionException("ResponseNotOk", submittedTaskInfoId, nodeIPAddress);
        }
        else
        {
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine($"HTTP GET successful for task ID: {submittedTaskInfoId}");
            logBuilder.AppendLine($"StatusCode: {(int)response.StatusCode} {response.StatusCode}");
            logBuilder.AppendLine($"Content: {response.Content}");
            logBuilder.AppendLine($"ResponseUri: {response.ResponseUri}");
            logBuilder.AppendLine($"RequestUri (resource): {request.Resource}");
            logBuilder.AppendLine($"AllocatedPort: {allocatedPort}");
            logBuilder.AppendLine($"NodeIPAddress: {nodeIPAddress}");
            logBuilder.AppendLine($"NodePort: {nodePort}");
            _logger.Info(logBuilder.ToString());
        }

        return response.Content;
    }

    public async Task<string> HttpPostToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeader> headers,
        string httpPayload, long submittedTaskInfoId, string nodeIPAddress, int nodePort, AdaptorUser loggedUser)
    {
        var taskInfo = _managementLogic.GetSubmittedTaskInfoById(submittedTaskInfoId, loggedUser);
        _logger.Info(
            $"HTTP POST from task: \"{submittedTaskInfoId}\" with remote node IP address: \"{nodeIPAddress}\" HTTP request: \"{httpRequest}\" HTTP headers: \"{string.Join(",", headers.Select(h=>$"({h.Name}, {h.Value})"))}\" HTTP Payload: \"{httpPayload}\"");

        var cluster = taskInfo.Specification.ClusterNodeType.Cluster;
        var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, taskInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id);
        var getTunnelsInfos = scheduler.GetTunnelsInfos(taskInfo, nodeIPAddress);

        if (!getTunnelsInfos.Any(f => f.RemotePort == nodePort))
            throw new UnableToCreateConnectionException("NoActiveConnection", submittedTaskInfoId, nodeIPAddress);

        var allocatedPort = getTunnelsInfos.First(f => f.RemotePort == nodePort).LocalPort.Value;
        var options = new RestClientOptions($"http://localhost:{allocatedPort}")
        {
            Encoding = Encoding.UTF8,
            CachePolicy = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true
            },
            Timeout = TimeSpan.FromMilliseconds(BusinessLogicConfiguration.HTTPRequestConnectionTimeoutInSeconds * 1000)
        };
        var basicRestClient = new RestClient(options);

        var request = new RestRequest(httpRequest, Method.Post);

        headers.ToList().ForEach(f => request.AddHeader(f.Name, f.Value));
        
        //Body part
        var payload = Encoding.UTF8.GetBytes(httpPayload);
        //check if headers contain content type
        if (!headers.Any(h => h.Name.ToLower() == "content-type"))
        {
            //if no content type is set, default to application/json
            request.AddHeader("content-type", "raw");
            _logger.Info($"No content-type header found, defaulting to 'raw' for task ID: {submittedTaskInfoId}");
        }
        
        //if content type is set to json, send json body, else send raw body
        if (headers.Any(h => h.Name.ToLower() == "content-type" && h.Value.ToLower().Contains("json")))
        {
            request.AddStringBody(httpPayload, DataFormat.Json);
            _logger.Info($"Adding JSON body for task ID: {submittedTaskInfoId}, Content-type: {string.Join(", ", headers.Where(h => h.Name.ToLower() == "content-type").Select(h => h.Value))}");
        }
        else
        {
            //default to raw
            request.AddBody(payload);
            _logger.Info($"Adding raw body for task ID: {submittedTaskInfoId}, Content-type: {string.Join(", ", headers.Where(h => h.Name.ToLower() == "content-type").Select(h => h.Value))}");
        }

        var response = await basicRestClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine($"HTTP POST failed for task ID: {submittedTaskInfoId}");
            logBuilder.AppendLine($"StatusCode: {(int)response.StatusCode} {response.StatusCode}");
            logBuilder.AppendLine($"Content: {response.Content}");
            logBuilder.AppendLine($"ErrorMessage: {response.ErrorMessage}");
            logBuilder.AppendLine($"ResponseUri: {response.ResponseUri}");
            logBuilder.AppendLine($"RequestUri (resource): {request.Resource}");
            logBuilder.AppendLine($"AllocatedPort: {allocatedPort}");
            logBuilder.AppendLine($"NodeIPAddress: {nodeIPAddress}");
            logBuilder.AppendLine($"NodePort: {nodePort}");
            logBuilder.AppendLine($"HTTP Payload: {httpPayload}");
            logBuilder.AppendLine($"HTTP Headers: {string.Join(", ", headers.Select(h => $"{h.Name}: {h.Value}"))}");
            logBuilder.AppendLine($"Request Body: {Encoding.UTF8.GetString(payload)}");
            logBuilder.AppendLine($"Content-Length: {payload.Length}");
            _logger.Info(logBuilder.ToString());

            throw new UnableToCreateConnectionException("ResponseNotOk", submittedTaskInfoId, nodeIPAddress);
        }
        else
        {
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine($"HTTP POST successful for task ID: {submittedTaskInfoId}");
            logBuilder.AppendLine($"StatusCode: {(int)response.StatusCode} {response.StatusCode}");
            //logBuilder.AppendLine($"Content: {response.Content}");
            logBuilder.AppendLine($"ResponseUri: {response.ResponseUri}");
            logBuilder.AppendLine($"RequestUri (resource): {request.Resource}");
            logBuilder.AppendLine($"AllocatedPort: {allocatedPort}");
            logBuilder.AppendLine($"NodeIPAddress: {nodeIPAddress}");
            logBuilder.AppendLine($"NodePort: {nodePort}");
            logBuilder.AppendLine($"HTTP Payload: {httpPayload}");
            logBuilder.AppendLine($"HTTP Headers: {string.Join(", ", headers.Select(h => $"{h.Name}: {h.Value}"))}");
            logBuilder.AppendLine($"Request Body: {Encoding.UTF8.GetString(payload)}");
            logBuilder.AppendLine($"Content-Length: {payload.Length}");
            _logger.Info(logBuilder.ToString());
        }

        return response.Content;
    }
    
    public async Task HttpPostToJobNodeStreamAsync(string httpRequest, IEnumerable<HTTPHeader> headers,
        string httpPayload, long submittedTaskInfoId, string nodeIPAddress, int nodePort, AdaptorUser loggedUser,
        Stream responseStream, CancellationToken cancellationToken)
    {
        var taskInfo = _managementLogic.GetSubmittedTaskInfoById(submittedTaskInfoId, loggedUser);
        _logger.Info($"HTTP POST (streaming) from task: \"{submittedTaskInfoId}\" with remote node IP: \"{nodeIPAddress}\"");

        var cluster = taskInfo.Specification.ClusterNodeType.Cluster;
        var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, taskInfo.Project);
        var getTunnelsInfos = scheduler.GetTunnelsInfos(taskInfo, nodeIPAddress);

        if (!getTunnelsInfos.Any(f => f.RemotePort == nodePort))
            throw new UnableToCreateConnectionException("NoActiveConnection", submittedTaskInfoId, nodeIPAddress);

        var allocatedPort = getTunnelsInfos.First(f => f.RemotePort == nodePort).LocalPort.Value;
        
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            ConnectTimeout = TimeSpan.FromSeconds(10),
            ResponseDrainTimeout = TimeSpan.FromSeconds(2)
        };
        
        using var httpClient = new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:{allocatedPort}{httpRequest}");
        
        foreach (var header in headers)
        {
            if (header.Name.Equals("content-type", StringComparison.OrdinalIgnoreCase))
            {
                requestMessage.Content = new StringContent(httpPayload ?? "", Encoding.UTF8, header.Value);
            }
            else
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Name, header.Value);
            }
        }

        if (requestMessage.Content == null)
        {
            requestMessage.Content = new StringContent(httpPayload ?? "", Encoding.UTF8, "application/json");
        }

        try
        {
            using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.Error($"HTTP POST failed for task {submittedTaskInfoId}: {response.StatusCode} - {errorContent}");
                throw new UnableToCreateConnectionException("ResponseNotOk", submittedTaskInfoId, nodeIPAddress);
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            var isEventStream = contentType.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase);

            _logger.Info($"Response content-type: {contentType}, streaming mode for task {submittedTaskInfoId}");

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            
            var buffer = new byte[256];
            int bytesRead;
            long totalBytes = 0;
            
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                totalBytes += bytesRead;
                
                if (isEventStream)
                {
                    await responseStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                }
                else
                {
                    var chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var sseData = $"data: {chunk}\n\n";
                    var sseBytes = Encoding.UTF8.GetBytes(sseData);
                    await responseStream.WriteAsync(sseBytes.AsMemory(), cancellationToken);
                }
                
                await responseStream.FlushAsync(cancellationToken);
                
                if (totalBytes % 10240 == 0)
                {
                    _logger.Debug($"Streamed {totalBytes} bytes for task {submittedTaskInfoId}");
                }
            }

            _logger.Info($"Streaming completed: {totalBytes} bytes for task {submittedTaskInfoId}");
        }
        catch (OperationCanceledException)
        {
            _logger.Info($"Streaming cancelled for task {submittedTaskInfoId}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Streaming failed for task {submittedTaskInfoId}: {ex.Message}", ex);
            throw;
        }
    }

    #endregion
}