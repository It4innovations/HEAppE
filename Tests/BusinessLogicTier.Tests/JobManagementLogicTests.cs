using System;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using Xunit;
using FluentAssertions;

namespace HEAppE.BusinessLogicTier.Tests;

[Trait("Category", "Unit")]
public class JobManagementLogicTests
{
    [Fact]
    public void TaskState_Transitions_FollowExpectedEnumHierarchy()
    {
        var submitted = TaskState.Submitted;
        var queued = TaskState.Queued;
        var running = TaskState.Running;
        var finished = TaskState.Finished;

        ((int)submitted).Should().BeLessThan((int)queued);
        ((int)queued).Should().BeLessThan((int)running);
        ((int)running).Should().BeLessThan((int)finished);
    }

    [Fact]
    public void TaskPriority_Values_AreOrderedCorrectly()
    {
        ((int)TaskPriority.Low).Should().BeLessThan((int)TaskPriority.Average);
        ((int)TaskPriority.Average).Should().BeLessThan((int)TaskPriority.High);
    }
}
