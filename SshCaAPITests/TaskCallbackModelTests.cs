using Xunit;
using System.Text.Json;
using HEAppE.RestApiModels.JobManagement;

namespace SshCaAPITests;

public class TaskCallbackModelTests
{
    [Fact]
    public void TaskCallbackModel_DeserializesFlatPayloadCorrectly()
    {
        var json = "{\"task_id\":\"123\",\"token\":\"token123\",\"raw_response\":\"{\\\"state\\\":\\\"finished\\\"}\",\"state\":\"finished\"}";
        var model = JsonSerializer.Deserialize<TaskCallbackModel>(json);

        Assert.NotNull(model);
        Assert.Equal("123", model.ScheduledJobId);
        Assert.Equal("token123", model.Token);
        Assert.Equal("{\"state\":\"finished\"}", model.RawResponse);
        Assert.Equal("finished", model.QSchedulerState);
        Assert.Null(model.GetTaskOrSessionId());
    }

    [Fact]
    public void TaskCallbackModel_DeserializesNestedTaskPayloadCorrectly()
    {
        var json = "{\"event\":\"task\",\"task\":{\"id\":456,\"state\":\"finished\",\"exectime_ms\":1500},\"token\":\"token123\"}";
        var model = JsonSerializer.Deserialize<TaskCallbackModel>(json);

        Assert.NotNull(model);
        Assert.Equal("token123", model.Token);
        Assert.Equal("task", model.Event);
        Assert.NotNull(model.TaskElement);
        Assert.Null(model.SessionElement);

        Assert.Equal("456", model.GetTaskOrSessionId());
        Assert.Equal("finished", model.GetState());
        Assert.Equal("{\"id\":456,\"state\":\"finished\",\"exectime_ms\":1500}", model.GetRawResponse());
    }

    [Fact]
    public void TaskCallbackModel_DeserializesNestedSessionPayloadCorrectly()
    {
        var json = "{\"event\":\"session\",\"session\":{\"id\":789,\"state\":\"opened\"},\"token\":\"token123\"}";
        var model = JsonSerializer.Deserialize<TaskCallbackModel>(json);

        Assert.NotNull(model);
        Assert.Equal("token123", model.Token);
        Assert.Equal("session", model.Event);
        Assert.Null(model.TaskElement);
        Assert.NotNull(model.SessionElement);

        Assert.Equal("789", model.GetTaskOrSessionId());
        Assert.Equal("opened", model.GetState());
        Assert.Equal("{\"id\":789,\"state\":\"opened\"}", model.GetRawResponse());
    }
}
