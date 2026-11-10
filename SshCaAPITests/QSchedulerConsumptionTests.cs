using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.Exceptions.External;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.DTO;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.Generic;
using HEAppE.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace SshCaAPITests;

/// <summary>
///     Consumption of QScheduler tasks and sessions, and isolation of HEAppE instances sharing one QScheduler.
/// </summary>
public class QSchedulerConsumptionTests
{
    private static Cluster CreateCluster(Dictionary<string, string> customConfiguration)
    {
        return new Cluster
        {
            Id = 1,
            MasterNodeName = "localhost",
            Port = 3000,
            ConnectionProtocol = ClusterConnectionProtocol.Http,
            CustomConfiguration = customConfiguration
        };
    }

    private static Project CreateQpuProject(string accountingString = "OPEN-33-12")
    {
        return new Project
        {
            Id = 10,
            Name = "Quantum project",
            AccountingString = accountingString,
            UsageType = HEAppE.DomainObjects.JobReporting.Enums.UsageType.QPUSeconds,
            ProjectClusterNodeTypeAggregations = new List<ProjectClusterNodeTypeAggregation>
            {
                new()
                {
                    AllocationAmount = 5000,
                    ClusterNodeTypeAggregation = new ClusterNodeTypeAggregation { Name = "QPUSeconds" }
                }
            }
        };
    }

    #region Project prefix

    [Fact]
    public void BuildQSchedulerProjectName_ExplicitPrefix_WinsOverNotifier()
    {
        var cluster = CreateCluster(new Dictionary<string, string>
        {
            { "QSchedulerProjectPrefix", "lexis-prod" },
            { "QSchedulerNotifier", "notifier-name" }
        });

        var name = QSchedulerSchedulerAdapter.BuildQSchedulerProjectName(cluster, CreateQpuProject());

        Assert.Equal("lexis-prod.OPEN-33-12", name);
    }

    [Fact]
    public void BuildQSchedulerProjectName_NoExplicitPrefix_UsesNotifier()
    {
        var cluster = CreateCluster(new Dictionary<string, string> { { "QSchedulerNotifier", "lexis-test" } });

        var name = QSchedulerSchedulerAdapter.BuildQSchedulerProjectName(cluster, CreateQpuProject());

        Assert.Equal("lexis-test.OPEN-33-12", name);
    }

    [Fact]
    public void BuildQSchedulerProjectName_PrefixWithSeparator_Throws()
    {
        var cluster = CreateCluster(new Dictionary<string, string> { { "QSchedulerProjectPrefix", "lexis.prod" } });

        Assert.Throws<ArgumentException>(() => QSchedulerSchedulerAdapter.BuildQSchedulerProjectName(cluster, CreateQpuProject()));
    }

    [Fact]
    public void BuildQSchedulerProjectName_NoAccountingString_Throws()
    {
        var cluster = CreateCluster(new Dictionary<string, string> { { "QSchedulerProjectPrefix", "lexis-prod" } });

        Assert.Throws<ArgumentException>(() => QSchedulerSchedulerAdapter.BuildQSchedulerProjectName(cluster, CreateQpuProject(accountingString: "")));
    }

    [Fact]
    public void BuildQSchedulerProjectName_TooLong_Throws()
    {
        var cluster = CreateCluster(new Dictionary<string, string> { { "QSchedulerProjectPrefix", new string('p', 120) } });

        Assert.Throws<ArgumentException>(() => QSchedulerSchedulerAdapter.BuildQSchedulerProjectName(cluster, CreateQpuProject(new string('a', 150))));
    }

    #endregion

    #region Task consumption

    [Fact]
    public void ReadParametersFromResponse_FinishedTask_MapsExecTimeToAllocatedSeconds()
    {
        var convertor = new QSchedulerDataConvertor(NullLogger.Instance);
        var response = "{\"id\":5,\"project\":\"lexis-prod.OPEN-33-12\",\"state\":\"finished\",\"started_at\":\"2026-11-02T10:00:00Z\"," +
                       "\"finished_at\":\"2026-11-02T10:00:01.5Z\",\"exectime_ms\":1500,\"user\":\"jdoe\",\"machine\":\"qpu\"}";

        var task = convertor.ReadParametersFromResponse(null, response).Single();

        Assert.Equal(TaskState.Finished, task.State);
        Assert.Equal(1.5, task.AllocatedTime);
        Assert.Equal(new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc), task.StartTime);
        Assert.Equal(new DateTime(2026, 11, 2, 10, 0, 1, 500, DateTimeKind.Utc), task.EndTime);
        Assert.Contains("exectime_ms=1500", task.AllParameters);
    }

    [Fact]
    public void ReadParametersFromResponse_RunningTask_ProvidesZeroExecTime()
    {
        var convertor = new QSchedulerDataConvertor(NullLogger.Instance);
        var response = "{\"id\":5,\"state\":\"running\",\"started_at\":\"2026-11-02T10:00:00Z\"}";

        var task = convertor.ReadParametersFromResponse(null, response).Single();

        Assert.Equal(TaskState.Running, task.State);
        Assert.Null(task.AllocatedTime);
        Assert.Contains("exectime_ms=0", task.AllParameters);
    }

    private static SubmittedTaskInfo CreateTaskWithAccounting(string scheduledJobId, string allParameters, string formula = "exectime_ms/1000")
    {
        var accounting = new Accounting { Id = 1, Formula = formula, ValidityFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var aggregation = new ClusterNodeTypeAggregation
        {
            ClusterNodeTypeAggregationAccountings = new List<ClusterNodeTypeAggregationAccounting>
            {
                new() { Accounting = accounting }
            }
        };

        return new SubmittedTaskInfo
        {
            Id = 7,
            ScheduledJobId = scheduledJobId,
            StartTime = new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc),
            AllParameters = allParameters,
            NodeType = new ClusterNodeType { ClusterNodeTypeAggregation = aggregation }
        };
    }

    [Fact]
    public void ComputeAccounting_DirectTask_ChargesExecTimeInQpuSeconds()
    {
        var task = CreateTaskWithAccounting("task:5", "id=5 state=finished exectime_ms=1500 time_limit_ms=60000");

        ResourceAccountingUtils.ComputeAccounting(task, task, NullLogger.Instance);

        Assert.NotNull(task.ResourceConsumed);
        Assert.Equal(1.5, task.ResourceConsumed.Value);
    }

    [Fact]
    public void ComputeAccounting_TaskInSession_IsNotChargedItself()
    {
        var task = CreateTaskWithAccounting("session:3:task:5", "id=5 state=finished exectime_ms=1500");

        ResourceAccountingUtils.ComputeAccounting(task, task, NullLogger.Instance);

        Assert.Null(task.ResourceConsumed);
        Assert.True(ResourceAccountingUtils.IsBilledThroughQSchedulerSession(task));
    }

    #endregion

    #region Session consumption

    [Fact]
    public void SessionInfo_Parse_ReadsConsumptionAndTimes()
    {
        var info = QSchedulerSessionInfo.Parse("{\"id\":3,\"state\":\"closed\",\"machine\":\"qpu\",\"project\":\"lexis-prod.OPEN-33-12\"," +
                                               "\"time_limit_ms\":60000,\"created_at\":\"2026-11-02T09:59:00Z\",\"opened_at\":\"2026-11-02T10:00:00Z\"," +
                                               "\"closed_at\":\"2026-11-02T10:05:00Z\",\"exectime_ms\":300000}");

        Assert.NotNull(info);
        Assert.Equal(3, info.Id);
        Assert.True(info.IsClosed);
        Assert.Equal("lexis-prod.OPEN-33-12", info.Project);
        Assert.Equal(300000, info.ExecTimeMs);
        Assert.Equal(new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc), info.OpenedAt);
        Assert.Equal(new DateTime(2026, 11, 2, 10, 5, 0, DateTimeKind.Utc), info.ClosedAt);
    }

    [Fact]
    public void SessionInfo_Parse_InvalidJson_ReturnsNull()
    {
        Assert.Null(QSchedulerSessionInfo.Parse("not json"));
        Assert.Null(QSchedulerSessionInfo.Parse(null));
    }

    [Fact]
    public void SessionInfo_ApplyTo_OpenSession_UpdatesConsumptionButNotFinal()
    {
        var dbSession = new QSchedulerSession { SessionId = 3, State = QSchedulerSessionState.Open };

        var changed = new QSchedulerSessionInfo { State = "open", ExecTimeMs = 1000, OpenedAt = DateTime.UtcNow }.ApplyTo(dbSession);

        Assert.True(changed);
        Assert.Equal(1000, dbSession.ConsumedMs);
        Assert.False(dbSession.IsConsumptionFinal);
        Assert.Equal(QSchedulerSessionState.Open, dbSession.State);
    }

    [Fact]
    public void SessionInfo_ApplyTo_OlderSmallerValue_IsIgnored()
    {
        var dbSession = new QSchedulerSession { SessionId = 3, State = QSchedulerSessionState.Open, ConsumedMs = 5000 };

        new QSchedulerSessionInfo { State = "open", ExecTimeMs = 1000 }.ApplyTo(dbSession);

        Assert.Equal(5000, dbSession.ConsumedMs);
    }

    [Fact]
    public void SessionInfo_ApplyTo_ClosedSession_IsFinal()
    {
        var closedAt = new DateTime(2026, 11, 2, 10, 5, 0, DateTimeKind.Utc);
        var dbSession = new QSchedulerSession { SessionId = 3, State = QSchedulerSessionState.Open, ConsumedMs = 1000 };

        new QSchedulerSessionInfo { State = "closed", ExecTimeMs = 300000, ClosedAt = closedAt }.ApplyTo(dbSession);

        Assert.Equal(300000, dbSession.ConsumedMs);
        Assert.Equal(QSchedulerSessionState.Closed, dbSession.State);
        Assert.Equal(closedAt, dbSession.ClosedAt);
        Assert.True(dbSession.IsConsumptionFinal);
    }

    [Fact]
    public void SessionInfo_ApplyTo_ClosedWithoutOpening_ConsumesNothing()
    {
        var dbSession = new QSchedulerSession { SessionId = 3, State = QSchedulerSessionState.Waiting };

        new QSchedulerSessionInfo { State = "closed" }.ApplyTo(dbSession);

        Assert.Equal(0, dbSession.ConsumedMs);
        Assert.True(dbSession.IsConsumptionFinal);
    }

    #endregion

    #region Project sync (HEAppE is the source of truth)

    private static readonly DateTime SyncNow = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    private static Project CreateDatedProject(string accountingString, long allocation, DateTime start, DateTime end)
    {
        var project = CreateQpuProject(accountingString);
        project.StartDate = start;
        project.EndDate = end;
        project.ProjectClusterNodeTypeAggregations[0].AllocationAmount = allocation;
        return project;
    }

    [Fact]
    public void ProjectInfo_ParseList_ReadsQSchedulerProjects()
    {
        var projects = QSchedulerProjectInfo.ParseList(
            "[{\"name\":\"lexis-prod.OPEN-1\",\"consumed_ms\":10,\"limit_ms\":5000000,\"active\":true}," +
            "{\"name\":\"other\",\"consumed_ms\":0,\"limit_ms\":1,\"active\":false}]");

        Assert.Equal(2, projects.Count);
        Assert.Equal("lexis-prod.OPEN-1", projects[0].Name);
        Assert.Equal(5000000, projects[0].LimitMs);
        Assert.True(projects[0].Active);
        Assert.False(projects[1].Active);
        Assert.Empty(QSchedulerProjectInfo.ParseList("not json"));
    }

    [Fact]
    public void SyncPlan_InSync_NoUpdates()
    {
        var heappe = new[] { CreateDatedProject("OPEN-1", 5000, SyncNow.AddDays(-1), SyncNow.AddDays(30)) };
        var qs = new[] { new QSchedulerProjectInfo { Name = "lexis-prod.OPEN-1", LimitMs = 5_000_000, Active = true } };

        Assert.Empty(QSchedulerProjectSync.Plan("lexis-prod", qs, heappe, SyncNow));
    }

    [Fact]
    public void SyncPlan_LimitChangedElsewhere_RestoresHeappeValue()
    {
        var heappe = new[] { CreateDatedProject("OPEN-1", 5000, SyncNow.AddDays(-1), SyncNow.AddDays(30)) };
        var qs = new[] { new QSchedulerProjectInfo { Name = "lexis-prod.OPEN-1", LimitMs = 99_000_000, Active = true } };

        var update = Assert.Single(QSchedulerProjectSync.Plan("lexis-prod", qs, heappe, SyncNow));

        Assert.Equal(5_000_000, update.LimitMs);
        Assert.Null(update.Active);
    }

    [Fact]
    public void SyncPlan_ExpiredProject_IsDeactivated()
    {
        var heappe = new[] { CreateDatedProject("OPEN-1", 5000, SyncNow.AddDays(-60), SyncNow.AddDays(-1)) };
        var qs = new[] { new QSchedulerProjectInfo { Name = "lexis-prod.OPEN-1", LimitMs = 5_000_000, Active = true } };

        var update = Assert.Single(QSchedulerProjectSync.Plan("lexis-prod", qs, heappe, SyncNow));

        Assert.False(update.Active);
        Assert.Null(update.LimitMs);
    }

    [Fact]
    public void SyncPlan_NotStartedProject_IsDeactivated()
    {
        var heappe = new[] { CreateDatedProject("OPEN-1", 5000, SyncNow.AddDays(1), SyncNow.AddDays(30)) };
        var qs = new[] { new QSchedulerProjectInfo { Name = "lexis-prod.OPEN-1", LimitMs = 5_000_000, Active = true } };

        Assert.False(Assert.Single(QSchedulerProjectSync.Plan("lexis-prod", qs, heappe, SyncNow)).Active);
    }

    [Fact]
    public void SyncPlan_ExtendedProject_IsReactivated()
    {
        var heappe = new[] { CreateDatedProject("OPEN-1", 5000, SyncNow.AddDays(-60), SyncNow.AddDays(30)) };
        var qs = new[] { new QSchedulerProjectInfo { Name = "lexis-prod.OPEN-1", LimitMs = 5_000_000, Active = false } };

        Assert.True(Assert.Single(QSchedulerProjectSync.Plan("lexis-prod", qs, heappe, SyncNow)).Active);
    }

    [Fact]
    public void SyncPlan_ProjectGoneFromHeappe_IsDeactivated()
    {
        var qs = new[] { new QSchedulerProjectInfo { Name = "lexis-prod.OPEN-OLD", LimitMs = 1000, Active = true } };

        var update = Assert.Single(QSchedulerProjectSync.Plan("lexis-prod", qs, Array.Empty<Project>(), SyncNow));

        Assert.Equal("lexis-prod.OPEN-OLD", update.QSchedulerProjectName);
        Assert.False(update.Active);
    }

    [Fact]
    public void SyncPlan_OtherInstancesProjects_AreNeverTouched()
    {
        var qs = new[]
        {
            new QSchedulerProjectInfo { Name = "lexis-test.OPEN-1", LimitMs = 1, Active = true },
            new QSchedulerProjectInfo { Name = "OPEN-1", LimitMs = 1, Active = true },
            new QSchedulerProjectInfo { Name = "lexis-prod-2.OPEN-1", LimitMs = 1, Active = true }
        };

        Assert.Empty(QSchedulerProjectSync.Plan("lexis-prod", qs, Array.Empty<Project>(), SyncNow));
    }

    [Fact]
    public async Task UpdateProjectAsync_SendsOnlyChangedFields()
    {
        var handler = new Mock<HttpMessageHandler>();
        string? sentBody = null;
        string? sentPath = null;
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Patch),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                sentPath = req.RequestUri!.AbsolutePath;
                sentBody = req.Content!.ReadAsStringAsync().Result;
            })
            .ReturnsAsync(() => new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") });
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler.Object, disposeHandler: false));
        var adapter = new QSchedulerSchedulerAdapter(new Mock<ISchedulerDataConvertor>().Object, factory.Object, NullLogger.Instance);

        await adapter.UpdateProjectAsync(new HttpConnection("http://localhost:3000"), CreateCallbackCluster(), "lexis-prod.OPEN-1", null, false);

        Assert.Equal("/projects/lexis-prod.OPEN-1", sentPath);
        Assert.Equal("{\"active\":false}", sentBody);
    }

    #endregion

    #region Reconciliation (lost callbacks)

    private static QSchedulerSchedulerAdapter CreateAdapterForTaskQuery(HttpStatusCode status, string body)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath.Contains("/tasks/")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage { StatusCode = status, Content = new StringContent(body) });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler.Object, disposeHandler: false));

        return new QSchedulerSchedulerAdapter(new QSchedulerDataConvertor(NullLogger.Instance), factory.Object, NullLogger.Instance);
    }

    private static Cluster CreateCallbackCluster()
    {
        return CreateCluster(new Dictionary<string, string> { { "EnableCallback", "true" }, { "QSchedulerProjectPrefix", "lexis-prod" } });
    }

    [Fact]
    public async Task GetActualTasksInfoAsync_ForcedQuery_ReturnsFinishedTaskWithExecTime()
    {
        var adapter = CreateAdapterForTaskQuery(HttpStatusCode.OK,
            "{\"id\":5,\"state\":\"finished\",\"started_at\":\"2026-11-02T10:00:00Z\",\"finished_at\":\"2026-11-02T10:00:01.5Z\",\"exectime_ms\":1500}");
        var task = new SubmittedTaskInfo { Id = 7, ScheduledJobId = "task:5", State = TaskState.Running, ForceStatusQuery = true };

        var result = (await adapter.GetActualTasksInfoAsync(new HttpConnection("http://localhost:3000"), CreateCallbackCluster(), new[] { task }, "svc")).Single();

        Assert.NotSame(task, result);
        Assert.Equal(TaskState.Finished, result.State);
        Assert.Equal(1.5, result.AllocatedTime);
        Assert.Contains("exectime_ms=1500", result.AllParameters);
        Assert.Equal(TaskState.Running, task.State); // the stored task is merged by the caller, not changed here
    }

    [Fact]
    public async Task GetActualTasksInfoAsync_ForcedQueryError_ThrowsInsteadOfFailingTask()
    {
        var adapter = CreateAdapterForTaskQuery(HttpStatusCode.NotFound, "task not found");
        var task = new SubmittedTaskInfo { Id = 7, ScheduledJobId = "task:5", State = TaskState.Running, ForceStatusQuery = true };

        var ex = await Assert.ThrowsAsync<HEAppE.Exceptions.Internal.QSchedulerApiException>(() =>
            adapter.GetActualTasksInfoAsync(new HttpConnection("http://localhost:3000"), CreateCallbackCluster(), new[] { task }, "svc"));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        Assert.Equal(TaskState.Running, task.State);
    }

    [Fact]
    public async Task GetActualTasksInfoAsync_PollingError_StillFailsTask()
    {
        var adapter = CreateAdapterForTaskQuery(HttpStatusCode.InternalServerError, "boom");
        var cluster = CreateCluster(new Dictionary<string, string> { { "QSchedulerProjectPrefix", "lexis-prod" } }); // callbacks off
        var task = new SubmittedTaskInfo { Id = 7, ScheduledJobId = "task:5", State = TaskState.Running };

        var result = (await adapter.GetActualTasksInfoAsync(new HttpConnection("http://localhost:3000"), cluster, new[] { task }, "svc")).Single();

        Assert.Equal(TaskState.Failed, result.State);
    }

    #endregion

    #region Sessions of other instances

    private static (QSchedulerSchedulerAdapter Adapter, Mock<HttpMessageHandler> Handler) CreateAdapterReturningSession(string sessionJson)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.Contains("/projects")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage { StatusCode = HttpStatusCode.Created, Content = new StringContent("") });
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath.Contains("/sessions/")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(sessionJson) });
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.Contains("/tasks")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage { StatusCode = HttpStatusCode.Created, Content = new StringContent("55") });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler.Object, disposeHandler: false));

        var adapter = new QSchedulerSchedulerAdapter(new Mock<ISchedulerDataConvertor>().Object, factory.Object, NullLogger.Instance);
        return (adapter, handler);
    }

    private static JobSpecification CreateJobUsingExistingSession(Cluster cluster, Project project, long sessionId)
    {
        var jobSpec = new JobSpecification
        {
            Id = 50,
            Cluster = cluster,
            Project = project,
            ProjectId = project.Id,
            Submitter = new AdaptorUser { Username = "jdoe" },
            ClusterUser = new ClusterAuthenticationCredentials { Username = "svc" },
            Tasks = new List<TaskSpecification>()
        };
        var taskSpec = new TaskSpecification
        {
            Id = 100,
            Name = "task",
            WalltimeLimit = 60,
            ClusterNodeType = new ClusterNodeType { Queue = "qpu", Cluster = cluster },
            JobSpecification = jobSpec,
            EnvironmentVariables = new List<EnvironmentVariable>
            {
                new() { Name = "HEAPPE_QSCHEDULER_SESSION_ID", Value = sessionId.ToString() }
            }
        };
        jobSpec.Tasks.Add(taskSpec);
        return jobSpec;
    }

    [Fact]
    public async Task SubmitJobAsync_SessionOfAnotherInstance_IsRejected()
    {
        var cluster = CreateCluster(new Dictionary<string, string> { { "QSchedulerProjectPrefix", "lexis-prod" } });
        var project = CreateQpuProject();
        var (adapter, _) = CreateAdapterReturningSession("{\"id\":3,\"state\":\"open\",\"project\":\"lexis-test.OPEN-33-12\"}");

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            adapter.SubmitJobAsync(new HttpConnection("http://localhost:3000"), CreateJobUsingExistingSession(cluster, project, 3), null));
    }

    [Fact]
    public async Task SubmitJobAsync_OwnOpenSession_SubmitsTaskIntoIt()
    {
        var cluster = CreateCluster(new Dictionary<string, string> { { "QSchedulerProjectPrefix", "lexis-prod" } });
        var project = CreateQpuProject();
        var (adapter, _) = CreateAdapterReturningSession("{\"id\":3,\"state\":\"open\",\"project\":\"lexis-prod.OPEN-33-12\"}");

        var jobSpec = CreateJobUsingExistingSession(cluster, project, 3);
        HEAppE.Utils.QSchedulerPayloadContext.StoreByName("task", System.Text.Encoding.UTF8.GetBytes("{}"));
        try
        {
            var results = (await adapter.SubmitJobAsync(new HttpConnection("http://localhost:3000"), jobSpec, null)).ToList();

            Assert.Single(results);
            Assert.Equal("session:3:task:55", results[0].ScheduledJobId);
        }
        finally
        {
            HEAppE.Utils.QSchedulerPayloadContext.RemoveByName("task");
        }
    }

    #endregion
}
