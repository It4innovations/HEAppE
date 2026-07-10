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
    [Required]
    [StringLength(100)]
    public string ScheduledJobId { get; set; }

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
}
