using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.Events.Models;

/// <summary>
/// CloudEvent specification 1.0 representation for HEAppE real-time WebSocket events.
/// </summary>
[DataContract(Name = "CloudEventExt")]
[Description("CloudEvent specification 1.0 representation for HEAppE real-time WebSocket events.")]
public class CloudEventExt
{
    /// <summary>
    /// Version of the CloudEvents specification (1.0).
    /// </summary>
    [DataMember(Name = "specversion")]
    [Description("Version of the CloudEvents specification (1.0).")]
    public string SpecVersion { get; set; } = "1.0";

    /// <summary>
    /// Type of occurrence related to the event (e.g. org.heappe.job.state-changed).
    /// </summary>
    [DataMember(Name = "type")]
    [Description("Type of occurrence related to the event (e.g. org.heappe.job.state-changed).")]
    public string Type { get; set; }

    /// <summary>
    /// Identifies the context in which an event happened.
    /// </summary>
    [DataMember(Name = "source")]
    [Description("Identifies the context in which an event happened.")]
    public string Source { get; set; }

    /// <summary>
    /// Identifies the event.
    /// </summary>
    [DataMember(Name = "id")]
    [Description("Identifies the event.")]
    public string Id { get; set; }

    /// <summary>
    /// Timestamp of when the event occurred.
    /// </summary>
    [DataMember(Name = "time")]
    [Description("Timestamp of when the event occurred.")]
    public string Time { get; set; }

    /// <summary>
    /// Content type of the data value.
    /// </summary>
    [DataMember(Name = "datacontenttype")]
    [Description("Content type of the data value.")]
    public string DataContentType { get; set; } = "application/json";

    /// <summary>
    /// The event payload.
    /// </summary>
    [DataMember(Name = "data")]
    [Description("The event payload.")]
    public object Data { get; set; }
}
