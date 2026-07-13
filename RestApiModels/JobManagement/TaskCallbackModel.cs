using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HEAppE.RestApiModels.JobManagement;

public class JsonStringOrNumberConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt64(out long l))
            {
                return l.ToString();
            }
            if (reader.TryGetDouble(out double d))
            {
                return d.ToString();
            }
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }
        using (JsonDocument document = JsonDocument.ParseValue(ref reader))
        {
            return document.RootElement.GetRawText();
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

/// <summary>
/// Model to callback update task status
/// </summary>
[DataContract(Name = "TaskCallbackModel")]
public class TaskCallbackModel
{
    /// <summary>
    /// Task ID in the scheduler (ScheduledJobId)
    /// </summary>
    [DataMember(Name = "task_id")]
    [JsonPropertyName("task_id")]
    [JsonConverter(typeof(JsonStringOrNumberConverter))]
    [StringLength(100)]
    public string? ScheduledJobId { get; set; }

    /// <summary>
    /// Session ID in QScheduler
    /// </summary>
    [DataMember(Name = "session_id")]
    [JsonPropertyName("session_id")]
    [JsonConverter(typeof(JsonStringOrNumberConverter))]
    [StringLength(100)]
    public string? SessionId { get; set; }

    /// <summary>
    /// Security token (CallbackSecret or machine token)
    /// </summary>
    [DataMember(Name = "token")]
    [JsonPropertyName("token")]
    [Required]
    [StringLength(100)]
    public string Token { get; set; }

    /// <summary>
    /// Raw payload response for DataConverter parser (optional)
    /// </summary>
    [DataMember(Name = "raw_response")]
    [JsonPropertyName("raw_response")]
    public string? RawResponse { get; set; }

    /// <summary>
    /// State string from QScheduler (optional, e.g. finished, failed...)
    /// </summary>
    [DataMember(Name = "state")]
    [JsonPropertyName("state")]
    public string? QSchedulerState { get; set; }

    /// <summary>
    /// Callback event type (optional, e.g. task, session)
    /// </summary>
    [DataMember(Name = "event")]
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    /// <summary>
    /// Nested task object from QScheduler callback
    /// </summary>
    [DataMember(Name = "task")]
    [JsonPropertyName("task")]
    public JsonElement? TaskElement { get; set; }

    /// <summary>
    /// Nested session object from QScheduler callback
    /// </summary>
    [DataMember(Name = "session")]
    [JsonPropertyName("session")]
    public JsonElement? SessionElement { get; set; }

    public string? GetTaskOrSessionId()
    {
        if (TaskElement != null && TaskElement.Value.ValueKind != JsonValueKind.Null && TaskElement.Value.ValueKind != JsonValueKind.Undefined)
        {
            return GetIdFromElement(TaskElement.Value);
        }
        if (SessionElement != null && SessionElement.Value.ValueKind != JsonValueKind.Null && SessionElement.Value.ValueKind != JsonValueKind.Undefined)
        {
            return GetIdFromElement(SessionElement.Value);
        }
        return null;
    }

    public string? GetState()
    {
        if (TaskElement != null && TaskElement.Value.ValueKind != JsonValueKind.Null && TaskElement.Value.ValueKind != JsonValueKind.Undefined)
        {
            return GetStateFromElement(TaskElement.Value);
        }
        if (SessionElement != null && SessionElement.Value.ValueKind != JsonValueKind.Null && SessionElement.Value.ValueKind != JsonValueKind.Undefined)
        {
            return GetStateFromElement(SessionElement.Value);
        }
        return null;
    }

    public string? GetRawResponse()
    {
        if (TaskElement != null && TaskElement.Value.ValueKind != JsonValueKind.Null && TaskElement.Value.ValueKind != JsonValueKind.Undefined)
        {
            return TaskElement.Value.GetRawText();
        }
        if (SessionElement != null && SessionElement.Value.ValueKind != JsonValueKind.Null && SessionElement.Value.ValueKind != JsonValueKind.Undefined)
        {
            return SessionElement.Value.GetRawText();
        }
        return null;
    }

    private string? GetIdFromElement(JsonElement element)
    {
        if (element.TryGetProperty("id", out var idProp))
        {
            if (idProp.ValueKind == JsonValueKind.Number)
            {
                return idProp.GetInt64().ToString();
            }
            if (idProp.ValueKind == JsonValueKind.String)
            {
                return idProp.GetString();
            }
        }
        return null;
    }

    private string? GetStateFromElement(JsonElement element)
    {
        if (element.TryGetProperty("state", out var stateProp))
        {
            return stateProp.GetString();
        }
        return null;
    }
}
