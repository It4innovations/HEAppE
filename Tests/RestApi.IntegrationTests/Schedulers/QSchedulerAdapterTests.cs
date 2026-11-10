using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.Schedulers;

[Trait("Category", "Unit")]
public class QSchedulerAdapterTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly Mock<ISchedulerDataConvertor> _mockConvertor;
    private readonly QSchedulerSchedulerAdapter _adapter;

    public QSchedulerAdapterTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHandler = new Mock<HttpMessageHandler>();
        _mockConvertor = new Mock<ISchedulerDataConvertor>();

        _adapter = new QSchedulerSchedulerAdapter(
            _mockConvertor.Object,
            _mockHttpClientFactory.Object,
            NullLogger.Instance
        );
    }

    private void SetupHttp(HttpStatusCode statusCode, string content)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });

        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_mockHandler.Object));
    }

    [Fact]
    public async Task CancelJobAsync_DirectTask_SendsDeleteRequest()
    {
        SetupHttp(HttpStatusCode.Accepted, "{\"status\": \"cancelled\"}");

        var cluster = new Cluster { Id = 1, MasterNodeName = "localhost" };
        var nodeType = new ClusterNodeType { Cluster = cluster };
        var tasks = new List<SubmittedTaskInfo>
        {
            new SubmittedTaskInfo
            {
                Id = 1,
                ScheduledJobId = "task:42",
                NodeType = nodeType
            }
        };

        var directConn = new HttpConnection("http://localhost:4300");

        await _adapter.CancelJobAsync(directConn, tasks, "User cancelled");

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete && req.RequestUri!.ToString().Contains("/tasks/42")),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task CancelJobAsync_Session_SendsDeleteSessionRequest()
    {
        SetupHttp(HttpStatusCode.Accepted, "{\"status\": \"closed\"}");

        var cluster = new Cluster { Id = 1, MasterNodeName = "localhost" };
        var nodeType = new ClusterNodeType { Cluster = cluster };
        var tasks = new List<SubmittedTaskInfo>
        {
            new SubmittedTaskInfo
            {
                Id = 1,
                ScheduledJobId = "session:99",
                NodeType = nodeType
            }
        };

        var directConn = new HttpConnection("http://localhost:4300");

        await _adapter.CancelJobAsync(directConn, tasks, "User closed session");

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete && req.RequestUri!.ToString().Contains("/sessions/99")),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetMachineArtifactAsync_ForwardsAcceptAndReturnsBackendContentType()
    {
        var payload = new byte[] { 0x08, 0x96, 0x01, 0xFF };
        HttpRequestMessage captured = null;
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(() =>
            {
                var content = new ByteArrayContent(payload);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/protobuf");
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = content };
            });
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_mockHandler.Object));

        var cluster = new Cluster { Id = 1, MasterNodeName = "localhost" };
        var directConn = new HttpConnection("http://localhost:4300");

        using var artifact = await _adapter.GetMachineArtifactAsync(directConn, cluster, "garnet", "settings", "application/protobuf");

        captured.RequestUri!.AbsolutePath.Should().Be("/machine/garnet/artifacts/settings");
        captured.Headers.Accept.ToString().Should().Be("application/protobuf");
        artifact.ContentType.Should().Be("application/protobuf");
        using var ms = new System.IO.MemoryStream();
        await artifact.Content.CopyToAsync(ms);
        ms.ToArray().Should().Equal(payload);
    }

    [Fact]
    public async Task GetMachineArtifactAsync_UnknownArtifact_ThrowsQSchedulerApiException()
    {
        SetupHttp(HttpStatusCode.NotFound, "Unknown artifact \"bogus\"");

        var cluster = new Cluster { Id = 1, MasterNodeName = "localhost" };
        var directConn = new HttpConnection("http://localhost:4300");

        var act = () => _adapter.GetMachineArtifactAsync(directConn, cluster, "garnet", "bogus", null);

        (await act.Should().ThrowAsync<QSchedulerApiException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(null, "/machine/garnet/calibration/default")]
    [InlineData("", "/machine/garnet/calibration/default")]
    [InlineData("readout", "/machine/garnet/calibration/default/readout")]
    public async Task GetMachineCalibrationAsync_EndpointIsOptional(string endpoint, string expectedPath)
    {
        HttpRequestMessage captured = null;
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_mockHandler.Object));

        var cluster = new Cluster { Id = 1, MasterNodeName = "localhost" };
        var directConn = new HttpConnection("http://localhost:4300");

        var result = await _adapter.GetMachineCalibrationAsync(directConn, cluster, "garnet", "default", endpoint);

        result.Should().Be("{}");
        captured.RequestUri!.AbsolutePath.Should().Be(expectedPath);
    }

    [Fact]
    public async Task GetActualTasksInfoAsync_QueriesTaskAndMapsState()
    {
        SetupHttp(HttpStatusCode.OK, "{\"state\": \"finished\", \"allocated_time\": 2.5}");

        _mockConvertor.Setup(c => c.ReadParametersFromResponse(It.IsAny<Cluster>(), It.IsAny<object>()))
            .Returns(new[] { new SubmittedTaskInfo { State = TaskState.Finished, AllocatedTime = 2.5 } });

        var cluster = new Cluster { Id = 1, MasterNodeName = "localhost" };
        var tasks = new List<SubmittedTaskInfo>
        {
            new SubmittedTaskInfo { Id = 10, ScheduledJobId = "task:500" }
        };

        var directConn = new HttpConnection("http://localhost:4300");

        var results = (await _adapter.GetActualTasksInfoAsync(directConn, cluster, tasks, "key")).ToList();

        results.Should().ContainSingle();
        results[0].State.Should().Be(TaskState.Finished);
    }

    [Fact]
    public async Task SubmitJobAsync_HttpError_ThrowsQSchedulerApiException()
    {
        SetupHttp(HttpStatusCode.InternalServerError, "Backend crashed");

        var cluster = new Cluster { Id = 1, MasterNodeName = "localhost" };
        var project = new Project
        {
            Id = 1,
            Name = "QProj",
            AccountingString = "QProj",
            UsageType = HEAppE.DomainObjects.JobReporting.Enums.UsageType.QPUSeconds,
            ProjectClusterNodeTypeAggregations = new List<ProjectClusterNodeTypeAggregation>
            {
                new ProjectClusterNodeTypeAggregation
                {
                    AllocationAmount = 1000,
                    ClusterNodeTypeAggregation = new ClusterNodeTypeAggregation { Name = "QPUSeconds" }
                }
            }
        };

        var jobSpec = new JobSpecification
        {
            Id = 1,
            Cluster = cluster,
            Project = project,
            Tasks = new List<TaskSpecification>
            {
                new TaskSpecification
                {
                    Id = 1,
                    ClusterNodeType = new ClusterNodeType { Queue = "TestMachine" }
                }
            }
        };

        var directConn = new HttpConnection("http://localhost:4300");

        var act = () => _adapter.SubmitJobAsync(directConn, jobSpec, null);
        await act.Should().ThrowAsync<QSchedulerApiException>();
    }
    private static JobSpecification BuildDirectJobSpec(params EnvironmentVariable[] envVars)
    {
        return new JobSpecification
        {
            Id = 1,
            Cluster = new Cluster
            {
                Id = 1,
                MasterNodeName = "localhost",
                CustomConfiguration = new Dictionary<string, string> { { "QSchedulerProjectPrefix", "test" } }
            },
            Project = new Project
            {
                Id = 1,
                Name = "QProjKind", // distinct name: the adapter caches registered projects statically
                AccountingString = "QProjKind",
                UsageType = HEAppE.DomainObjects.JobReporting.Enums.UsageType.QPUSeconds,
                ProjectClusterNodeTypeAggregations = new List<ProjectClusterNodeTypeAggregation>
                {
                    new ProjectClusterNodeTypeAggregation
                    {
                        AllocationAmount = 1000,
                        ClusterNodeTypeAggregation = new ClusterNodeTypeAggregation { Name = "QPUSeconds" }
                    }
                }
            },
            Tasks = new List<TaskSpecification>
            {
                new TaskSpecification
                {
                    Id = 1,
                    Name = "t1",
                    ClusterNodeType = new ClusterNodeType { Queue = "garnet" },
                    EnvironmentVariables = new List<EnvironmentVariable>(envVars)
                }
            }
        };
    }

    private List<HttpRequestMessage> CaptureTaskSubmits()
    {
        var captured = new List<HttpRequestMessage>();
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured.Add(req))
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                // An existing session is checked to belong to this instance's project before tasks are submitted into it.
                Content = new StringContent(req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath.StartsWith("/sessions/")
                    ? "{\"id\":5,\"state\":\"open\",\"project\":\"test.QProjKind\"}"
                    : "7")
            });
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_mockHandler.Object));
        return captured;
    }

    [Fact]
    public async Task SubmitJobAsync_PulseTask_SendsKindPulseAndProtobufContentType()
    {
        var captured = CaptureTaskSubmits();
        var jobSpec = BuildDirectJobSpec(new EnvironmentVariable { Name = "HEAPPE_QSCHEDULER_TASK_KIND", Value = "pulse" });
        var payload = new byte[] { 0x0A, 0x00, 0xFF, 0x80 };
        HEAppE.Utils.QSchedulerPayloadContext.StoreByName("t1", payload);
        try
        {
            await _adapter.SubmitJobAsync(new HttpConnection("http://localhost:4300"), jobSpec, null);
        }
        finally
        {
            HEAppE.Utils.QSchedulerPayloadContext.RemoveByName("t1");
        }

        var submit = captured.Should().ContainSingle(r => r.RequestUri!.AbsolutePath == "/tasks").Subject;
        submit.RequestUri!.Query.Should().Contain("kind=pulse");
        submit.Content!.Headers.ContentType!.MediaType.Should().Be("application/protobuf");
    }

    [Fact]
    public async Task SubmitJobAsync_CircuitTask_OmitsKindAndSendsJson()
    {
        var captured = CaptureTaskSubmits();
        var jobSpec = BuildDirectJobSpec();
        HEAppE.Utils.QSchedulerPayloadContext.StoreByName("t1", Encoding.UTF8.GetBytes("{}"));
        try
        {
            await _adapter.SubmitJobAsync(new HttpConnection("http://localhost:4300"), jobSpec, null);
        }
        finally
        {
            HEAppE.Utils.QSchedulerPayloadContext.RemoveByName("t1");
        }

        var submit = captured.Should().ContainSingle(r => r.RequestUri!.AbsolutePath == "/tasks").Subject;
        submit.RequestUri!.Query.Should().NotContain("kind=");
        submit.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }
    [Fact]
    public async Task SubmitJobAsync_PulseTaskInOpenSession_SendsKindPulseAndProtobufContentType()
    {
        var captured = CaptureTaskSubmits();
        var jobSpec = BuildDirectJobSpec(
            new EnvironmentVariable { Name = "HEAPPE_QSCHEDULER_SESSION_ID", Value = "5" },
            new EnvironmentVariable { Name = "HEAPPE_QSCHEDULER_TASK_KIND", Value = "pulse" });
        HEAppE.Utils.QSchedulerPayloadContext.StoreByName("t1", new byte[] { 0x0A, 0xFF });
        try
        {
            await _adapter.SubmitJobAsync(new HttpConnection("http://localhost:4300"), jobSpec, null);
        }
        finally
        {
            HEAppE.Utils.QSchedulerPayloadContext.RemoveByName("t1");
        }

        var submit = captured.Should().ContainSingle(r => r.RequestUri!.AbsolutePath == "/tasks").Subject;
        submit.RequestUri!.Query.Should().Contain("session_id=5").And.Contain("kind=pulse");
        submit.Content!.Headers.ContentType!.MediaType.Should().Be("application/protobuf");
    }
}
