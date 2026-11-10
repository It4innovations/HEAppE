using System;
using FluentAssertions;
using HEAppE.BusinessLogicTier.Logic.JobManagement;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using Xunit;

namespace HEAppE.BusinessLogicTier.Tests;

/// <summary>
///     Rules for checking QScheduler tasks directly when their callback may have been lost.
/// </summary>
[Trait("Category", "Unit")]
public class QSchedulerReconcileTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

    private static (SubmittedTaskInfo Task, SubmittedJobInfo Job) CreateTask(TaskState state, DateTime submitTime, DateTime? stateUpdatedAt,
        DateTime? startTime = null, int walltimeSeconds = 3600)
    {
        var job = new SubmittedJobInfo { Id = 1, SubmitTime = submitTime };
        var task = new SubmittedTaskInfo
        {
            Id = 2,
            State = state,
            ScheduledJobId = "task:5",
            StateUpdatedAt = stateUpdatedAt,
            StartTime = startTime,
            Specification = new TaskSpecification { WalltimeLimit = walltimeSeconds }
        };
        return (task, job);
    }

    [Fact]
    public void RecentStateChange_IsNotDue()
    {
        var (task, job) = CreateTask(TaskState.Running, Now.AddMinutes(-30), Now.AddMinutes(-2), Now.AddMinutes(-2));

        JobManagementLogic.IsQSchedulerReconcileDue(task, job, Now, lastReconciledAt: null).Should().BeFalse();
    }

    [Fact]
    public void NoStateChangeLongerThanRetryWindow_IsDue()
    {
        var (task, job) = CreateTask(TaskState.Running, Now.AddMinutes(-30), Now.AddMinutes(-11), Now.AddMinutes(-11));

        JobManagementLogic.IsQSchedulerReconcileDue(task, job, Now, lastReconciledAt: null).Should().BeTrue();
    }

    [Fact]
    public void NoStateTimestamp_UsesSubmitTime()
    {
        var (task, job) = CreateTask(TaskState.Submitted, Now.AddMinutes(-11), stateUpdatedAt: null);

        JobManagementLogic.IsQSchedulerReconcileDue(task, job, Now, lastReconciledAt: null).Should().BeTrue();
    }

    [Fact]
    public void RecentlyChecked_IsNotDueAgainYet()
    {
        var (task, job) = CreateTask(TaskState.Running, Now.AddHours(-2), Now.AddHours(-2), Now.AddHours(-2), walltimeSeconds: 24 * 3600);

        JobManagementLogic.IsQSchedulerReconcileDue(task, job, Now, lastReconciledAt: Now.AddMinutes(-2)).Should().BeFalse();
        JobManagementLogic.IsQSchedulerReconcileDue(task, job, Now, lastReconciledAt: Now.AddMinutes(-6)).Should().BeTrue();
    }

    [Fact]
    public void AboutToTimeOut_IsDueEvenIfJustChecked()
    {
        // Short walltime: the task would be failed by the timeout before the normal 10-minute window.
        var (task, job) = CreateTask(TaskState.Running, Now.AddMinutes(-5), Now.AddMinutes(-3), Now.AddMinutes(-3), walltimeSeconds: 60);

        JobManagementLogic.IsQSchedulerReconcileDue(task, job, Now, lastReconciledAt: Now.AddSeconds(-30)).Should().BeTrue();
    }

    [Fact]
    public void Timeout_RunningPastWalltimePlusGrace()
    {
        var (task, job) = CreateTask(TaskState.Running, Now.AddMinutes(-10), Now.AddMinutes(-3), Now.AddSeconds(-121), walltimeSeconds: 60);

        JobManagementLogic.GetCallbackTimeoutMessage(task, job, Now).Should().Contain("exceeded walltime limit of 60s");
    }

    [Fact]
    public void Timeout_NotStartedWithinThreeWalltimes()
    {
        var (task, job) = CreateTask(TaskState.Queued, Now.AddSeconds(-181), Now.AddSeconds(-181), walltimeSeconds: 60);

        JobManagementLogic.GetCallbackTimeoutMessage(task, job, Now).Should().Contain("3x walltime");
    }

    [Fact]
    public void NoTimeout_WithinLimits()
    {
        var (task, job) = CreateTask(TaskState.Running, Now.AddMinutes(-5), Now.AddMinutes(-1), Now.AddMinutes(-1), walltimeSeconds: 3600);

        JobManagementLogic.GetCallbackTimeoutMessage(task, job, Now).Should().BeNull();
    }
}
