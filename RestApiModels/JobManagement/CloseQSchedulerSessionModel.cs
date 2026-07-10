using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model for closing a QScheduler session explicitly
/// </summary>
[DataContract(Name = "CloseQSchedulerSessionModel")]
[Description("Model for closing a QScheduler session explicitly")]
public class CloseQSchedulerSessionModel : SessionCodeModel
{
    /// <summary>
    /// Cluster ID
    /// </summary>
    [DataMember(Name = "ClusterId")]
    [Description("Cluster ID")]
    public long ClusterId { get; set; }

    /// <summary>
    /// Project ID
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Project ID")]
    public long ProjectId { get; set; }

    /// <summary>
    /// Session ID to close
    /// </summary>
    [DataMember(Name = "SessionId")]
    [Description("Session ID to close")]
    public long SessionId { get; set; }
}
