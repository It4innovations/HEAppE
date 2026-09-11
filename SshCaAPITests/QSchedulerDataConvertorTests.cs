using Xunit;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.Generic;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;

namespace SshCaAPITests;

public class QSchedulerDataConvertorTests
{
    [Fact]
    public void ReadParametersFromResponse_WaitingState_ReturnsQueued()
    {
        var convertor = new QSchedulerDataConvertor(NullLogger.Instance);
        var response = "{\"state\": \"waiting\"}";
        
        var results = convertor.ReadParametersFromResponse(null, response).ToList();
        
        Assert.Single(results);
        Assert.Equal(TaskState.Queued, results[0].State);
        Assert.Null(results[0].ErrorMessage);
    }

    [Fact]
    public void ReadParametersFromResponse_RunningState_ReturnsRunning()
    {
        var convertor = new QSchedulerDataConvertor(NullLogger.Instance);
        var response = "{\"state\": \"running\"}";
        
        var results = convertor.ReadParametersFromResponse(null, response).ToList();
        
        Assert.Single(results);
        Assert.Equal(TaskState.Running, results[0].State);
    }

    [Fact]
    public void ReadParametersFromResponse_FinishedState_ReturnsFinished()
    {
        var convertor = new QSchedulerDataConvertor(NullLogger.Instance);
        var response = "{\"state\": \"finished\"}";
        
        var results = convertor.ReadParametersFromResponse(null, response).ToList();
        
        Assert.Single(results);
        Assert.Equal(TaskState.Finished, results[0].State);
    }

    [Fact]
    public void ReadParametersFromResponse_FailedState_ReturnsFailedWithError()
    {
        var convertor = new QSchedulerDataConvertor(NullLogger.Instance);
        var response = "{\"state\": \"failed\", \"error\": \"insufficient qubits\"}";
        
        var results = convertor.ReadParametersFromResponse(null, response).ToList();
        
        Assert.Single(results);
        Assert.Equal(TaskState.Failed, results[0].State);
        Assert.Equal("insufficient qubits", results[0].ErrorMessage);
    }

    [Fact]
    public void ReadParametersFromResponse_StartedState_ReturnsRunning()
    {
        var convertor = new QSchedulerDataConvertor(NullLogger.Instance);
        var response = "{\"state\": \"started\"}";
        
        var results = convertor.ReadParametersFromResponse(null, response).ToList();
        
        Assert.Single(results);
        Assert.Equal(TaskState.Running, results[0].State);
    }

    [Fact]
    public void ReadParametersFromResponse_DetailedJson_ParsesTimestampsAndAllocatedTime()
    {
        var convertor = new QSchedulerDataConvertor(NullLogger.Instance);
        var response = "{\"state\": \"finished\", \"start_time\": \"2026-07-13T06:56:09Z\", \"end_time\": \"2026-07-13T06:56:10.5Z\", \"allocated_time\": 1.5, \"extra_metric\": 42, \"nested\": {\"val\": \"test\"}}";
        
        var results = convertor.ReadParametersFromResponse(null, response).ToList();
        
        Assert.Single(results);
        var task = results[0];
        Assert.Equal(TaskState.Finished, task.State);
        Assert.Equal(new System.DateTime(2026, 7, 13, 6, 56, 9, System.DateTimeKind.Utc), task.StartTime);
        Assert.Equal(new System.DateTime(2026, 7, 13, 6, 56, 10, 500, System.DateTimeKind.Utc), task.EndTime);
        Assert.Equal(1.5, task.AllocatedTime);
        Assert.Contains("state=finished", task.AllParameters);
        Assert.Contains("allocated_time=1.5", task.AllParameters);
        Assert.Contains("extra_metric=42", task.AllParameters);
        Assert.Contains("nested.val=test", task.AllParameters);
    }

    [Fact]
    public void ReadParametersFromResponse_UnixTimestamps_ParsesUnixSecondsAndMilliseconds()
    {
        var convertor = new QSchedulerDataConvertor(NullLogger.Instance);
        var response = "{\"state\": \"finished\", \"started_at\": 1783856169, \"finished_at\": 1783856170123}";
        
        var results = convertor.ReadParametersFromResponse(null, response).ToList();
        
        Assert.Single(results);
        var task = results[0];
        Assert.Equal(System.DateTimeOffset.FromUnixTimeSeconds(1783856169).UtcDateTime, task.StartTime);
        Assert.Equal(System.DateTimeOffset.FromUnixTimeMilliseconds(1783856170123).UtcDateTime, task.EndTime);
    }

    [Fact]
    public void GetJobIds_IntegerString_ReturnsId()
    {
        var convertor = new QSchedulerDataConvertor(NullLogger.Instance);
        var response = "42";
        
        var results = convertor.GetJobIds(response).ToList();
        
        Assert.Single(results);
        Assert.Equal("42", results[0]);
    }

    [Fact]
    public void ReadParametersFromResponse_ExecTimeMs_CalculatesStartTime()
    {
        var convertor = new QSchedulerDataConvertor(NullLogger.Instance);
        var response = "{\"state\": \"finished\", \"finished_at\": \"2026-07-13T06:56:10.000Z\", \"exectime_ms\": 2500}";
        
        var results = convertor.ReadParametersFromResponse(null, response).ToList();
        
        Assert.Single(results);
        var task = results[0];
        Assert.Equal(TaskState.Finished, task.State);
        Assert.Equal(new System.DateTime(2026, 7, 13, 6, 56, 10, System.DateTimeKind.Utc), task.EndTime);
        Assert.Equal(new System.DateTime(2026, 7, 13, 6, 56, 7, 500, System.DateTimeKind.Utc), task.StartTime);
    }

    [Fact]
    public void ReadParametersFromResponse_ExecTimeMsString_CalculatesStartTime()
    {
        var convertor = new QSchedulerDataConvertor(NullLogger.Instance);
        var response = "{\"state\": \"finished\", \"finished_at\": \"2026-07-13T06:56:10.000Z\", \"exectime_ms\": \"1500\"}";
        
        var results = convertor.ReadParametersFromResponse(null, response).ToList();
        
        Assert.Single(results);
        var task = results[0];
        Assert.Equal(TaskState.Finished, task.State);
        Assert.Equal(new System.DateTime(2026, 7, 13, 6, 56, 10, System.DateTimeKind.Utc), task.EndTime);
        Assert.Equal(new System.DateTime(2026, 7, 13, 6, 56, 8, 500, System.DateTimeKind.Utc), task.StartTime);
    }
}

