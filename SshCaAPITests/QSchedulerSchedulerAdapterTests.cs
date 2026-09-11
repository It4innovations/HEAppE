using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Moq.Protected;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.Generic;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace SshCaAPITests;

public class QSchedulerSchedulerAdapterTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly QSchedulerSchedulerAdapter _adapter;

    public QSchedulerSchedulerAdapterTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        var mockConvertor = new Mock<ISchedulerDataConvertor>();
        
        _adapter = new QSchedulerSchedulerAdapter(
            mockConvertor.Object,
            _mockHttpClientFactory.Object,
            NullLogger.Instance
        );
    }

    private JobSpecification CreateMockJobSpecification(Cluster cluster, Project project, string machineQueue, int walltimeLimit = 3600)
    {
        var nodeType = new ClusterNodeType
        {
            Queue = machineQueue,
            Cluster = cluster
        };

        var clusterUser = new ClusterAuthenticationCredentials
        {
            Username = "testuser"
        };

        // Use temp directory for scratch storage path in tests
        var tempScratch = Path.GetTempPath().Replace('\\', '/').TrimEnd('/');

        var clusterProject = new ClusterProject
        {
            ProjectId = project.Id,
            ClusterId = cluster.Id,
            ScratchStoragePath = tempScratch,
            ProjectStoragePath = tempScratch,
            Cluster = cluster
        };
        cluster.ClusterProjects = new List<ClusterProject> { clusterProject };

        var jobSpec = new JobSpecification
        {
            Id = 50,
            Cluster = cluster,
            Project = project,
            ProjectId = project.Id,
            ClusterUser = clusterUser,
            Tasks = new List<TaskSpecification>()
        };

        var taskSpec = new TaskSpecification
        {
            Id = 100,
            ClusterNodeType = nodeType,
            WalltimeLimit = walltimeLimit,
            JobSpecification = jobSpec
        };
        jobSpec.Tasks.Add(taskSpec);

        return jobSpec;
    }

    [Fact]
    public async Task SubmitJobAsync_QPUSecondsConfigured_RegistersProjectAndSubmitsTask()
    {
        // Arrange
        var cluster = new Cluster
        {
            Id = 1,
            MasterNodeName = "localhost",
            Port = 3000,
            ConnectionProtocol = ClusterConnectionProtocol.Http,
            CustomConfiguration = new Dictionary<string, string>
            {
                { "QSchedulerPort", "3000" }
            }
        };

        var project = new Project
        {
            Id = 10,
            Name = "MyProject",
            AccountingString = "MyProjectAcc",
            UsageType = HEAppE.DomainObjects.JobReporting.Enums.UsageType.QPUSeconds,
            ProjectClusterNodeTypeAggregations = new List<ProjectClusterNodeTypeAggregation>
            {
                new ProjectClusterNodeTypeAggregation
                {
                    AllocationAmount = 5000, // 5000 QPUSeconds
                    ClusterNodeTypeAggregation = new ClusterNodeTypeAggregation
                    {
                        Name = "QPUSeconds"
                    }
                }
            }
        };

        var jobSpec = CreateMockJobSpecification(cluster, project, "MachineSimulator", 3600);
        var directConn = new HttpConnection("http://localhost:3000");

        // Staging the payload file locally
        var taskDir = HEAppE.Utils.FileSystemUtils.GetTaskClusterDirectoryPath(jobSpec.Tasks.First(), "Identifier", "HEAppE/Executions").Replace('\\', '/');
        var payloadPath = $"{taskDir}/payload.json";
        Directory.CreateDirectory(Path.GetDirectoryName(payloadPath));
        System.IO.File.WriteAllText(payloadPath, "{}");

        try
        {
            // Mock HTTP calls:
            // 1. POST /projects -> 201 Created
            // 2. POST /tasks?machine_id=MachineSimulator -> 201 (Returns "123" as taskId)
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.AbsolutePath.Contains("/projects")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Created,
                    Content = new StringContent("Created")
                });

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.AbsolutePath.Contains("/tasks")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Created,
                    Content = new StringContent("123")
                });

            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(() => new HttpClient(_mockHttpMessageHandler.Object, disposeHandler: false));

            // Act
            var results = await _adapter.SubmitJobAsync(directConn, jobSpec, null);

            // Assert
            Assert.Single(results);
            var submittedTask = results.First();
            Assert.Equal("task:123", submittedTask.ScheduledJobId);
            Assert.Equal(TaskState.Submitted, submittedTask.State);
        }
        finally
        {
            if (System.IO.File.Exists(payloadPath))
            {
                System.IO.File.Delete(payloadPath);
            }
        }
    }

    [Fact]
    public async Task SubmitJobAsync_MissingQPUSeconds_ThrowsArgumentException()
    {
        // Arrange
        var cluster = new Cluster
        {
            Id = 1,
            MasterNodeName = "localhost",
            ConnectionProtocol = ClusterConnectionProtocol.Http
        };

        var project = new Project
        {
            Id = 10,
            Name = "MyProject",
            AccountingString = "MyProjectAcc",
            UsageType = HEAppE.DomainObjects.JobReporting.Enums.UsageType.QPUSeconds,
            ProjectClusterNodeTypeAggregations = new List<ProjectClusterNodeTypeAggregation>() // empty limit
        };

        var jobSpec = CreateMockJobSpecification(cluster, project, "MachineSimulator", 3600);
        var directConn = new HttpConnection("http://localhost:3000");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _adapter.SubmitJobAsync(directConn, jobSpec, null));
    }

    [Fact]
    public async Task SubmitJobAsync_WrongUsageType_ThrowsArgumentException()
    {
        // Arrange
        var cluster = new Cluster
        {
            Id = 1,
            MasterNodeName = "localhost",
            ConnectionProtocol = ClusterConnectionProtocol.Http
        };

        var project = new Project
        {
            Id = 10,
            Name = "MyProject",
            AccountingString = "MyProjectAcc",
            UsageType = HEAppE.DomainObjects.JobReporting.Enums.UsageType.NodeHours,
            ProjectClusterNodeTypeAggregations = new List<ProjectClusterNodeTypeAggregation>
            {
                new ProjectClusterNodeTypeAggregation
                {
                    AllocationAmount = 5000,
                    ClusterNodeTypeAggregation = new ClusterNodeTypeAggregation
                    {
                        Name = "QPUSeconds"
                    }
                }
            }
        };

        var jobSpec = CreateMockJobSpecification(cluster, project, "MachineSimulator", 3600);
        var directConn = new HttpConnection("http://localhost:3000");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _adapter.SubmitJobAsync(directConn, jobSpec, null));
    }

    [Fact]
    public async Task SubmitJobAsync_ProjectAlreadyExists_HandlesConflictGracefully()
    {
        // Arrange
        var cluster = new Cluster
        {
            Id = 1,
            MasterNodeName = "localhost",
            ConnectionProtocol = ClusterConnectionProtocol.Http
        };

        var project = new Project
        {
            Id = 10,
            Name = "MyProject",
            AccountingString = "MyProjectAcc",
            UsageType = HEAppE.DomainObjects.JobReporting.Enums.UsageType.QPUSeconds,
            ProjectClusterNodeTypeAggregations = new List<ProjectClusterNodeTypeAggregation>
            {
                new ProjectClusterNodeTypeAggregation
                {
                    AllocationAmount = 5000,
                    ClusterNodeTypeAggregation = new ClusterNodeTypeAggregation
                    {
                        Name = "QPUSeconds"
                    }
                }
            }
        };

        var jobSpec = CreateMockJobSpecification(cluster, project, "MachineSimulator", 3600);
        var directConn = new HttpConnection("http://localhost:3000");

        // Staging the payload file locally
        var taskDir = HEAppE.Utils.FileSystemUtils.GetTaskClusterDirectoryPath(jobSpec.Tasks.First(), "Identifier", "HEAppE/Executions").Replace('\\', '/');
        var payloadPath = $"{taskDir}/payload.json";
        Directory.CreateDirectory(Path.GetDirectoryName(payloadPath));
        System.IO.File.WriteAllText(payloadPath, "{}");

        try
        {
            // Mock POST /projects to return 409 Conflict
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.AbsolutePath.Contains("/projects")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Conflict,
                    Content = new StringContent("Project already exists")
                });

            // Mock PATCH /projects to succeed
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Patch && req.RequestUri.AbsolutePath.Contains("/projects")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}")
                });

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.AbsolutePath.Contains("/tasks")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Created,
                    Content = new StringContent("123")
                });

            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(() => new HttpClient(_mockHttpMessageHandler.Object, disposeHandler: false));

            // Act
            var results = await _adapter.SubmitJobAsync(directConn, jobSpec, null);

            // Assert
            Assert.Single(results);
            Assert.Equal("task:123", results.First().ScheduledJobId);
        }
        finally
        {
            if (System.IO.File.Exists(payloadPath))
            {
                System.IO.File.Delete(payloadPath);
            }
        }
    }

    [Fact]
    public async Task SubmitJobAsync_MultipleQPUSecondsConfigured_SumsAggregationsAndRegistersProject()
    {
        // Arrange
        var cluster = new Cluster
        {
            Id = 1,
            MasterNodeName = "localhost",
            Port = 3000,
            ConnectionProtocol = ClusterConnectionProtocol.Http,
            CustomConfiguration = new Dictionary<string, string>
            {
                { "QSchedulerPort", "3000" }
            }
        };

        var project = new Project
        {
            Id = 10,
            Name = "MyProject_MultipleAggregations",
            AccountingString = "MyProjectAcc_MultipleAggregations",
            UsageType = HEAppE.DomainObjects.JobReporting.Enums.UsageType.QPUSeconds,
            ProjectClusterNodeTypeAggregations = new List<ProjectClusterNodeTypeAggregation>
            {
                new ProjectClusterNodeTypeAggregation
                {
                    AllocationAmount = 3000,
                    ClusterNodeTypeAggregation = new ClusterNodeTypeAggregation
                    {
                        Name = "QPUSeconds"
                    }
                },
                new ProjectClusterNodeTypeAggregation
                {
                    AllocationAmount = 2000,
                    ClusterNodeTypeAggregation = new ClusterNodeTypeAggregation
                    {
                        Name = "QPUSeconds"
                    }
                }
            }
        };

        var jobSpec = CreateMockJobSpecification(cluster, project, "MachineSimulator", 3600);
        var directConn = new HttpConnection("http://localhost:3000");

        // Staging the payload file locally
        var taskDir = HEAppE.Utils.FileSystemUtils.GetTaskClusterDirectoryPath(jobSpec.Tasks.First(), "Identifier", "HEAppE/Executions").Replace('\\', '/');
        var payloadPath = $"{taskDir}/payload.json";
        Directory.CreateDirectory(Path.GetDirectoryName(payloadPath));
        System.IO.File.WriteAllText(payloadPath, "{}");

        try
        {
            // Verify that the limit payload registers 5000000 ms (converted from 3000 + 2000 = 5000 QPUSeconds)
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Post && 
                        req.RequestUri.AbsolutePath.Contains("/projects") && 
                        req.Content.ReadAsStringAsync().Result.Contains("\"limit_ms\":5000000")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Created,
                    Content = new StringContent("Created")
                })
                .Verifiable();

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.AbsolutePath.Contains("/tasks")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Created,
                    Content = new StringContent("123")
                });

            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(() => new HttpClient(_mockHttpMessageHandler.Object, disposeHandler: false));

            // Act
            var results = await _adapter.SubmitJobAsync(directConn, jobSpec, null);

            // Assert
            Assert.Single(results);
            Assert.Equal("task:123", results.First().ScheduledJobId);
            _mockHttpMessageHandler.Verify();
        }
        finally
        {
            if (System.IO.File.Exists(payloadPath))
            {
                System.IO.File.Delete(payloadPath);
            }
        }
    }
}
