using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.JobManagement;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.DataTransfer;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.Exceptions.External;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.Services.UserOrg;
using log4net;
using RestSharp;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier.Logic.DataTransfer;

public class DataTransferLogic : IDataTransferLogic
{
    private readonly ILog _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJobManagementLogic _managementLogic;
    private readonly IUserOrgService _userOrgService;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;

    private static readonly ConcurrentDictionary<long, List<ActiveTunnelState>> _activeTunnels = new();
    private static readonly ConcurrentDictionary<long, object> _taskLocks = new();

    public DataTransferLogic(IUnitOfWork unitOfWork, IUserOrgService userOrgService, 
        ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        _logger = LogManager.GetLogger(typeof(DataTransferLogic));
        _unitOfWork = unitOfWork;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _userOrgService = userOrgService;
        _managementLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
    }

    private class ActiveTunnelState
    {
        public long OwnerUserId { get; set; }
        public int LocalPort { get; set; }
        public int RemotePort { get; set; }
        public string NodeIP { get; set; }
    }

    private static object GetLockForTask(long taskId) => _taskLocks.GetOrAdd(taskId, _ => new object());

    private int GetUserSpecificLocalPort(long taskId, string nodeIP, int nodePort, long userId)
    {
        if (_activeTunnels.TryGetValue(taskId, out var tunnels))
        {
            var userTunnel = tunnels.LastOrDefault(t => 
                t.NodeIP == nodeIP && 
                t.RemotePort == nodePort && 
                t.OwnerUserId == userId);
            
            if (userTunnel != null) return userTunnel.LocalPort;
        }
        throw new UnableToCreateConnectionException("NoActiveConnectionForUser", taskId, nodeIP);
    }

    public DataTransferMethod GetDataTransferMethod(string nodeIPAddress, int nodePort, long submittedTaskInfoId, AdaptorUser loggedUser)
    {
        var taskInfo = _managementLogic.GetSubmittedTaskInfoById(submittedTaskInfoId, loggedUser, true);
        if (taskInfo.State != TaskState.Running)
            throw new UnableToCreateConnectionException("NotRunningTask", taskInfo.Id);

        var taskLock = GetLockForTask(submittedTaskInfoId);
        lock (taskLock)
        {
            var cluster = taskInfo.Specification.ClusterNodeType.Cluster;
            var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType)
                .CreateScheduler(cluster, taskInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id);

            scheduler.CreateTunnel(taskInfo, nodeIPAddress, nodePort, _httpContextKeys.Context.SshCaToken);

            var tunnelInfo = scheduler.GetTunnelsInfos(taskInfo, nodeIPAddress)
                .OrderByDescending(t => t.LocalPort)
                .FirstOrDefault(w => w.RemotePort == nodePort);

            if (tunnelInfo?.LocalPort == null)
                throw new UnableToCreateConnectionException("CreationFailed", submittedTaskInfoId, nodeIPAddress, nodePort);

            var tunnelState = new ActiveTunnelState {
                OwnerUserId = loggedUser.Id,
                LocalPort = tunnelInfo.LocalPort.Value,
                RemotePort = nodePort,
                NodeIP = nodeIPAddress
            };

            _activeTunnels.AddOrUpdate(submittedTaskInfoId, 
                new List<ActiveTunnelState> { tunnelState }, 
                (id, list) => { list.Add(tunnelState); return list; });

            return new DataTransferMethod {
                SubmittedTaskId = taskInfo.Id,
                Port = tunnelInfo.LocalPort,
                NodeIPAddress = tunnelInfo.NodeHost,
                NodePort = tunnelInfo.RemotePort
            };
        }
    }

    public void EndDataTransfer(DataTransferMethod transferMethod, AdaptorUser loggedUser)
    {
        var taskLock = GetLockForTask(transferMethod.SubmittedTaskId);
        lock (taskLock)
        {
            if (!_activeTunnels.TryGetValue(transferMethod.SubmittedTaskId, out var tunnels))
            {
                throw new UnableToCreateConnectionException("NoActiveConnection", transferMethod.SubmittedTaskId);
            }

            var tunnel = tunnels.FirstOrDefault(t => t.LocalPort == transferMethod.Port);
        
            if (tunnel == null)
            {
                throw new UnableToCreateConnectionException("ConnectionNotFound", transferMethod.SubmittedTaskId);
            }

            if (tunnel.OwnerUserId != loggedUser.Id)
            {
                _logger.Warn($"User {loggedUser.Id} attempted to close tunnel on port {transferMethod.Port} owned by user {tunnel.OwnerUserId}");
                throw new UnauthorizedAccessException($"Access denied: You do not have permission to close this tunnel. It belongs to user ID {tunnel.OwnerUserId}.");
            }

            var taskInfo = _managementLogic.GetSubmittedTaskInfoById(transferMethod.SubmittedTaskId, loggedUser, true);
            var cluster = taskInfo.Specification.ClusterNodeType.Cluster;
        
            SchedulerFactory.GetInstance(cluster.SchedulerType)
                .CreateScheduler(cluster, taskInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id)
                .RemoveTunnel(taskInfo, _httpContextKeys.Context.SshCaToken);

            tunnels.Remove(tunnel);
        
            if (!tunnels.Any())
            {
                _activeTunnels.TryRemove(transferMethod.SubmittedTaskId, out _);
                _taskLocks.TryRemove(transferMethod.SubmittedTaskId, out _);
            }
        
            _logger.Info($"Tunnel on port {transferMethod.Port} for task {transferMethod.SubmittedTaskId} successfully closed by owner {loggedUser.Id}.");
        }
    }

    public async Task<string> HttpGetToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeader> headers, long submittedTaskInfoId, string nodeIPAddress, int nodePort, AdaptorUser loggedUser)
    {
        int localPort = GetUserSpecificLocalPort(submittedTaskInfoId, nodeIPAddress, nodePort, loggedUser.Id);

        var options = new RestClientOptions($"http://localhost:{localPort}") {
            Encoding = Encoding.UTF8,
            Timeout = TimeSpan.FromSeconds(BusinessLogicConfiguration.HTTPRequestConnectionTimeoutInSeconds)
        };
        var client = new RestClient(options);
        var request = new RestRequest(httpRequest);
        foreach (var h in headers) request.AddHeader(h.Name, h.Value);

        var response = await client.ExecuteAsync(request);
        if (response.StatusCode != HttpStatusCode.OK) throw new UnableToCreateConnectionException("ResponseNotOk", response.Content);
        return response.Content;
    }

    public async Task<string> HttpPostToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeader> headers, string httpPayload, long submittedTaskInfoId, string nodeIPAddress, int nodePort, AdaptorUser loggedUser)
    {
        int localPort = GetUserSpecificLocalPort(submittedTaskInfoId, nodeIPAddress, nodePort, loggedUser.Id);

        var options = new RestClientOptions($"http://localhost:{localPort}") {
            Encoding = Encoding.UTF8,
            Timeout = TimeSpan.FromSeconds(BusinessLogicConfiguration.HTTPRequestConnectionTimeoutInSeconds)
        };
        var client = new RestClient(options);
        var request = new RestRequest(httpRequest, Method.Post);
        foreach (var h in headers) request.AddHeader(h.Name, h.Value);
        
        if (headers.Any(h => h.Name.ToLower() == "content-type" && h.Value.ToLower().Contains("json")))
            request.AddStringBody(httpPayload, DataFormat.Json);
        else
            request.AddBody(Encoding.UTF8.GetBytes(httpPayload));

        var response = await client.ExecuteAsync(request);
        if (response.StatusCode != HttpStatusCode.OK) throw new UnableToCreateConnectionException("ResponseNotOk", response.Content);
        return response.Content;
    }

    public async Task HttpPostToJobNodeStreamAsync(string httpRequest, IEnumerable<HTTPHeader> headers, string httpPayload, long submittedTaskInfoId, string nodeIPAddress, int nodePort, AdaptorUser loggedUser, Stream responseStream, CancellationToken cancellationToken)
    {
        int localPort = GetUserSpecificLocalPort(submittedTaskInfoId, nodeIPAddress, nodePort, loggedUser.Id);

        using var httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:{localPort}{httpRequest}");
        
        foreach (var h in headers) {
            if (h.Name.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                requestMessage.Content = new StringContent(httpPayload ?? "", Encoding.UTF8, h.Value);
            else
                requestMessage.Headers.TryAddWithoutValidation(h.Name, h.Value);
        }
        if (requestMessage.Content == null) requestMessage.Content = new StringContent(httpPayload ?? "", Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new UnableToCreateConnectionException("ResponseNotOk", response.Content);

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await contentStream.CopyToAsync(responseStream, cancellationToken);
    }

    public IEnumerable<long> GetTaskIdsWithOpenTunnels() => _activeTunnels.Keys;

    public void CloseAllTunnelsForTask(SubmittedTaskInfo taskInfo)
    {
        var taskLock = GetLockForTask(taskInfo.Id);
        lock (taskLock)
        {
            var scheduler = SchedulerFactory.GetInstance(taskInfo.Specification.JobSpecification.Cluster.SchedulerType)
                .CreateScheduler(taskInfo.Specification.JobSpecification.Cluster, taskInfo.Project, _sshCertificateAuthorityService, adaptorUserId: taskInfo.Specification.JobSpecification.Submitter.Id);
            scheduler.RemoveTunnel(taskInfo, _httpContextKeys.Context.SshCaToken);
            _activeTunnels.TryRemove(taskInfo.Id, out _);
            _taskLocks.TryRemove(taskInfo.Id, out _);
        }
    }
}