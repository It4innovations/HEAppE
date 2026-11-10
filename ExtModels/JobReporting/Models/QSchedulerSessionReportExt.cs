using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// QScheduler session usage report ext
/// </summary>
[DataContract(Name = "QSchedulerSessionReportExt")]
[Description("QScheduler session usage. A session is billed for its whole open time; tasks that ran in it have no usage of their own.")]
public class QSchedulerSessionReportExt
{
    /// <summary>
    /// QScheduler session id
    /// </summary>
    [DataMember]
    [Description("QScheduler session id")]
    public long SessionId { get; set; }

    /// <summary>
    /// Cluster id
    /// </summary>
    [DataMember]
    [Description("Cluster id")]
    public long ClusterId { get; set; }

    /// <summary>
    /// Id of the user who owns the session
    /// </summary>
    [DataMember]
    [Description("Id of the user who owns the session")]
    public long UserId { get; set; }

    /// <summary>
    /// Session state
    /// </summary>
    [DataMember]
    [Description("Session state")]
    public string State { get; set; }

    /// <summary>
    /// When the session opened
    /// </summary>
    [DataMember]
    [Description("When the session opened")]
    public DateTime? OpenedAt { get; set; }

    /// <summary>
    /// When the session closed
    /// </summary>
    [DataMember]
    [Description("When the session closed")]
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Usage (session open time in QPU seconds)
    /// </summary>
    [DataMember]
    [Description("Usage (session open time in QPU seconds)")]
    public double Usage { get; set; }

    /// <summary>
    /// True once the session is closed and its final usage is known
    /// </summary>
    [DataMember]
    [Description("True once the session is closed and its final usage is known")]
    public bool IsFinal { get; set; }
}
