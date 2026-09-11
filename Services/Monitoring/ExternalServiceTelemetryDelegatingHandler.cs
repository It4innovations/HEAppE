using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.DomainObjects.Monitoring;
using HEAppE.Utils;

namespace HEAppE.Services.Monitoring;

/// <summary>
/// DelegatingHandler for HttpClient that automatically records HTTP request latency, status code, and errors as telemetry.
/// </summary>
public class ExternalServiceTelemetryDelegatingHandler : DelegatingHandler
{
    private readonly string _serviceName;
    private readonly string _serviceType;
    private readonly IExternalServiceTelemetryService _telemetryService;

    public ExternalServiceTelemetryDelegatingHandler(
        string serviceName = "http-service",
        string serviceType = "http",
        IExternalServiceTelemetryService telemetryService = null)
    {
        _serviceName = serviceName;
        _serviceType = serviceType;
        _telemetryService = telemetryService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        HttpResponseMessage response = null;
        Exception capturedException = null;

        try
        {
            response = await base.SendAsync(request, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            capturedException = ex;
            throw;
        }
        finally
        {
            sw.Stop();
            try
            {
                var isSuccess = capturedException == null && (response?.IsSuccessStatusCode ?? false);
                var statusCode = response != null ? ((int)response.StatusCode).ToString() : "500";
                var pathAndQuery = request.RequestUri?.PathAndQuery ?? request.RequestUri?.ToString() ?? "unknown";
                var host = request.RequestUri?.Host ?? "unknown";
                var protocol = request.RequestUri?.Scheme ?? "http";

                var recorder = _telemetryService ?? ExternalServiceTelemetryService.Instance;
                recorder.Record(new ExternalServiceHealthLog
                {
                    ServiceName = _serviceName,
                    ServiceType = _serviceType,
                    Protocol = protocol,
                    EndpointOrHost = host,
                    CommandOrPath = pathAndQuery,
                    ResponseTimeMs = sw.ElapsedMilliseconds,
                    IsAvailable = isSuccess,
                    StatusCode = statusCode,
                    ErrorMessage = capturedException?.Message,
                    JobId = JobExecutionContext.CurrentJobId,
                    TaskId = JobExecutionContext.CurrentTaskId,
                    ClusterId = JobExecutionContext.CurrentClusterId,
                    RequestId = JobExecutionContext.CurrentRequestId,
                    Source = "Execution",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch
            {
                // Suppress telemetry handler exceptions
            }
        }
    }
}
