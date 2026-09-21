using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.DomainObjects.Monitoring;
using HEAppE.ExtModels.Management.Converts;
using HEAppE.Services.Monitoring;
using HEAppE.Utils;
using Moq;
using Moq.Protected;
using Xunit;

namespace SshCaAPITests;

public class ExternalServiceTelemetryTests
{
    [Fact]
    public void JobExecutionContext_Scope_SetsAndRestoresValues()
    {
        // Assert initial state is null
        Assert.Null(JobExecutionContext.CurrentJobId);
        Assert.Null(JobExecutionContext.CurrentTaskId);
        Assert.Null(JobExecutionContext.CurrentClusterId);
        Assert.Null(JobExecutionContext.CurrentRequestId);

        using (JobExecutionContext.BeginScope(jobId: 100, taskId: 200, clusterId: 300, requestId: "req-123"))
        {
            Assert.Equal(100, JobExecutionContext.CurrentJobId);
            Assert.Equal(200, JobExecutionContext.CurrentTaskId);
            Assert.Equal(300, JobExecutionContext.CurrentClusterId);
            Assert.Equal("req-123", JobExecutionContext.CurrentRequestId);

            // Nested scope
            using (JobExecutionContext.BeginScope(jobId: 101, taskId: 201))
            {
                Assert.Equal(101, JobExecutionContext.CurrentJobId);
                Assert.Equal(201, JobExecutionContext.CurrentTaskId);
                Assert.Equal(300, JobExecutionContext.CurrentClusterId); // Inherited from parent
                Assert.Equal("req-123", JobExecutionContext.CurrentRequestId); // Inherited from parent
            }

            // Restored after nested scope exit
            Assert.Equal(100, JobExecutionContext.CurrentJobId);
            Assert.Equal(200, JobExecutionContext.CurrentTaskId);
        }

        // Restored after outer scope exit
        Assert.Null(JobExecutionContext.CurrentJobId);
        Assert.Null(JobExecutionContext.CurrentTaskId);
        Assert.Null(JobExecutionContext.CurrentClusterId);
        Assert.Null(JobExecutionContext.CurrentRequestId);
    }

    [Fact]
    public void TelemetryService_Record_DropsWhenDisabled()
    {
        var settings = new ExternalServiceTelemetrySettings { IsEnabled = false };
        var service = new ExternalServiceTelemetryService(settings);

        service.Record(new ExternalServiceHealthLog
        {
            ServiceName = "test",
            ResponseTimeMs = 50,
            IsAvailable = true
        });

        Assert.False(service.Reader.TryRead(out _));
    }

    [Fact]
    public void TelemetryService_Record_QueuesWhenEnabled()
    {
        var settings = new ExternalServiceTelemetrySettings { IsEnabled = true, ChannelCapacity = 100 };
        var service = new ExternalServiceTelemetryService(settings);

        service.Record(new ExternalServiceHealthLog
        {
            ServiceName = "vault",
            ResponseTimeMs = 12,
            IsAvailable = true
        });

        Assert.True(service.Reader.TryRead(out var item));
        Assert.NotNull(item);
        Assert.Equal("vault", item.ServiceName);
        Assert.Equal(12, item.ResponseTimeMs);
        Assert.True(item.IsAvailable);
    }

    [Fact]
    public async Task DelegatingHandler_CapturesTelemetryAndAmbientContext()
    {
        var settings = new ExternalServiceTelemetrySettings { IsEnabled = true, ChannelCapacity = 100 };
        var telemetryService = new ExternalServiceTelemetryService(settings);

        var innerHandlerMock = new Mock<HttpMessageHandler>();
        innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"status\":\"ok\"}")
            });

        var handler = new ExternalServiceTelemetryDelegatingHandler("test-api", "service", telemetryService)
        {
            InnerHandler = innerHandlerMock.Object
        };

        var invoker = new HttpMessageInvoker(handler);

        using (JobExecutionContext.BeginScope(jobId: 555, requestId: "trace-xyz"))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/health");
            var response = await invoker.SendAsync(request, CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        Assert.True(telemetryService.Reader.TryRead(out var log));
        Assert.NotNull(log);
        Assert.Equal("test-api", log.ServiceName);
        Assert.Equal("service", log.ServiceType);
        Assert.Equal("https", log.Protocol);
        Assert.Equal("api.example.com", log.EndpointOrHost);
        Assert.Equal("/health", log.CommandOrPath);
        Assert.True(log.IsAvailable);
        Assert.Equal("200", log.StatusCode);
        Assert.Equal(555, log.JobId);
        Assert.Equal("trace-xyz", log.RequestId);
    }

    [Fact]
    public void ManagementConverts_ConvertIntToExt_MapsPropertiesCorrectly()
    {
        var log = new ExternalServiceHealthLog
        {
            Id = 1,
            ServiceName = "hashicorp vault",
            ServiceType = "keymanagement",
            Protocol = "https",
            EndpointOrHost = "vault.local",
            Port = 8200,
            CommandOrPath = "GetClusterSecret",
            Timestamp = DateTime.UtcNow,
            IsAvailable = true,
            ResponseTimeMs = 45,
            StatusCode = "200",
            ErrorMessage = null,
            JobId = 1234,
            TaskId = 5678,
            ClusterId = 9,
            RequestId = "trace-001",
            Source = "Execution"
        };

        var ext = log.ConvertIntToExt();

        Assert.Equal(log.Id, ext.Id);
        Assert.Equal(log.ServiceName, ext.ServiceName);
        Assert.Equal(log.ServiceType, ext.ServiceType);
        Assert.Equal(log.Protocol, ext.Protocol);
        Assert.Equal(log.EndpointOrHost, ext.EndpointOrHost);
        Assert.Equal(log.Port, ext.Port);
        Assert.Equal(log.CommandOrPath, ext.Operation);
        Assert.Equal(log.Timestamp, ext.Timestamp);
        Assert.True(ext.IsSuccess);
        Assert.Equal(log.ResponseTimeMs, ext.ResponseTimeMs);
        Assert.Equal("200", ext.StatusCode);
        Assert.Null(ext.ErrorMessage);
        Assert.Equal(1234, ext.JobId);
        Assert.Equal(5678, ext.TaskId);
        Assert.Equal(9, ext.ClusterId);
        Assert.Equal("trace-001", ext.RequestId);
        Assert.Equal("Execution", ext.Source);
    }
}
