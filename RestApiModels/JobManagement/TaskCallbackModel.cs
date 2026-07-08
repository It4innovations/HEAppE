using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace HEAppE.RestApiModels.JobManagement;

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
