using System;
using System.Linq;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.Schedulers;

[Trait("Category", "Unit")]
public class QSchedulerDataConvertorTests
{
    private readonly QSchedulerDataConvertor _convertor = new(NullLogger.Instance);

    [Theory]
    [InlineData("waiting", TaskState.Queued)]
    [InlineData("running", TaskState.Running)]
    [InlineData("started", TaskState.Running)]
    [InlineData("finished", TaskState.Finished)]
    [InlineData("failed", TaskState.Failed)]
    [InlineData("error", TaskState.Failed)]
    [InlineData("cancelled", TaskState.Canceled)]
    [InlineData("unknown_state", TaskState.Unknown)]
    public void ReadParametersFromResponse_MapsStateCorrectly(string stateJson, TaskState expectedState)
    {
        var response = $"{{\"state\": \"{stateJson}\"}}";
        var results = _convertor.ReadParametersFromResponse(null, response).ToList();

        results.Should().ContainSingle();
        results[0].State.Should().Be(expectedState);
    }

    [Fact]
    public void ReadParametersFromResponse_ErrorField_CapturedInErrorMessage()
    {
        var response = "{\"state\": \"failed\", \"error\": \"Qubit calibration expired\"}";
        var results = _convertor.ReadParametersFromResponse(null, response).ToList();

        results.Should().ContainSingle();
        results[0].State.Should().Be(TaskState.Failed);
        results[0].ErrorMessage.Should().Be("Qubit calibration expired");
    }

    [Fact]
    public void ReadParametersFromResponse_QpuSecondsAndAllocatedTime_ParsesCorrectly()
    {
        var response = "{\"state\": \"finished\", \"qpu_seconds\": 4.56}";
        var results = _convertor.ReadParametersFromResponse(null, response).ToList();

        results.Should().ContainSingle();
        results[0].AllocatedTime.Should().Be(4.56);
    }

    [Fact]
    public void ReadParametersFromResponse_BillableTimeMs_ParsesCorrectly()
    {
        var response = "{\"state\": \"finished\", \"billable_time_ms\": 2500}";
        var results = _convertor.ReadParametersFromResponse(null, response).ToList();

        results.Should().ContainSingle();
        results[0].AllocatedTime.Should().Be(2500);
    }

    [Fact]
    public void ReadParametersFromResponse_EmptyOrWhitespace_ReturnsEmpty()
    {
        var resultsNull = _convertor.ReadParametersFromResponse(null, null).ToList();
        var resultsEmpty = _convertor.ReadParametersFromResponse(null, "").ToList();
        var resultsWhitespace = _convertor.ReadParametersFromResponse(null, "   ").ToList();

        resultsNull.Should().BeEmpty();
        resultsEmpty.Should().BeEmpty();
        resultsWhitespace.Should().BeEmpty();
    }

    [Fact]
    public void ReadParametersFromResponse_InvalidJson_ThrowsFormatException()
    {
        var action = () => _convertor.ReadParametersFromResponse(null, "{not valid json").ToList();
        action.Should().Throw<FormatException>();
    }

    [Fact]
    public void GetJobIds_VariousFormats_ParsesJobId()
    {
        // Raw integer string
        _convertor.GetJobIds("101").Should().ContainSingle().Which.Should().Be("101");

        // JSON integer
        _convertor.GetJobIds(" 202 ").Should().ContainSingle().Which.Should().Be("202");

        // JSON object with "id"
        _convertor.GetJobIds("{\"id\": 303}").Should().ContainSingle().Which.Should().Be("303");

        // Empty string
        _convertor.GetJobIds("").Should().BeEmpty();
    }
}
