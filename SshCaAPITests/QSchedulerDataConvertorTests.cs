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
    public void GetJobIds_IntegerString_ReturnsId()
    {
        var convertor = new QSchedulerDataConvertor(NullLogger.Instance);
        var response = "42";
        
        var results = convertor.GetJobIds(response).ToList();
        
        Assert.Single(results);
        Assert.Equal("42", results[0]);
    }
}
