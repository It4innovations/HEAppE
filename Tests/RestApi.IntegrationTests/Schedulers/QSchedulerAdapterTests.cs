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
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete && req.RequestUri.ToString().Contains("/tasks/42")),
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
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete && req.RequestUri.ToString().Contains("/sessions/99")),
            ItExpr.IsAny<CancellationToken>()
        );
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
}
